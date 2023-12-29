using GameDevTV.Inventories;
using GameDevTV.Saving;
using RPG.Control;
using RPG.Inventories;
using RPG.Stats;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Shops {
    public class Shop : MonoBehaviour, IRaycastable, ISaveable {
        [SerializeField] string shopName;
        [SerializeField, Range(0, 100)] float sellingPercentage = 80f;

        //Stock Config
        [SerializeField] StockItemConfig[] stockConfig;

        [Serializable]
        class StockItemConfig {
            public InventoryItem item;
            public int initialStock;
            [Range(0, 100)] public float buyingDiscountPercentage;
            public int levelToUnlock = 0;
        }

        Dictionary<InventoryItem, int> transcation = new Dictionary<InventoryItem, int>();
        Dictionary<InventoryItem, int> stockSold = new Dictionary<InventoryItem, int>();
        Shopper currentShopper = null;
        bool isBuyingMode = true;
        ItemCategory filter = ItemCategory.None;

        public event Action onChange;

        public void SetShopper(Shopper shopper) {
            this.currentShopper = shopper;
        }

        public IEnumerable<ShopItem> GetFilteredItems() {
            foreach (ShopItem shopItem in GetAllItems()) {
                InventoryItem item = shopItem.GetInventoryItem();
                if (filter == ItemCategory.None || item.GetCategory() == filter) {
                    yield return shopItem;
                }
            }
        }

        public IEnumerable<ShopItem> GetAllItems() {
            Dictionary<InventoryItem, float> prices = GetPrices();
            Dictionary<InventoryItem, int> availabilities = GetAvailabilities();
            foreach (InventoryItem item in availabilities.Keys) {
                if (availabilities[item] <= 0) continue;

                float price = prices[item];
                int quantityInTransaction = 0;
                transcation.TryGetValue(item, out quantityInTransaction);
                int availability = availabilities[item];
                yield return new ShopItem(item, availability, price, quantityInTransaction);
            }
        }

        public void SelectFilter(ItemCategory category) {
            filter = category;
            print(category);

            if (onChange != null) {
                onChange();
            }
        }

        public ItemCategory GetFilter() {
            return filter;
        }

        public void SelectMode(bool isBuying) {
            isBuyingMode = isBuying;

            if (onChange != null) {
                onChange();
            }
        }

        public bool IsBuyingMode() {
            return isBuyingMode;
        }

        public bool CanTransact() {
            if (IsTransactionEmpty()) return false;
            if (!HasSufficientFunds()) return false;
            if (!HasInventorySpace()) return false;

            return true;
        }

        public bool HasSufficientFunds() {
            if (!isBuyingMode) return true;
            Purse purse = currentShopper.GetComponent<Purse>();
            if (purse == null) return false;

            return purse.GetBalance() >= TransactionTotal();
        }

        public bool IsTransactionEmpty() {
            return transcation.Count == 0;
        }

        public bool HasInventorySpace() {
            if (!isBuyingMode) return true;

            Inventory shopperInventory = currentShopper.GetComponent<Inventory>();
            if (shopperInventory == null) return false;

            List<InventoryItem> flatItems = new List<InventoryItem>();
            foreach (ShopItem shopItem in GetAllItems()) {
                InventoryItem item = shopItem.GetInventoryItem();
                int quantity = shopItem.GetQuantityInTransaction();
                for (int i = 0; i < quantity; i++) {
                    flatItems.Add(item);
                }
            }

            return shopperInventory.HasSpaceFor(flatItems);
        }

        public void ConfirmTransaction() {
            Inventory shopperInventory = currentShopper.GetComponent<Inventory>();
            Purse shopperPurse = currentShopper.GetComponent<Purse>();
            if (shopperInventory == null || shopperPurse == null) return;

            foreach (ShopItem shopItem in GetAllItems()) {
                InventoryItem item = shopItem.GetInventoryItem();
                int quantity = shopItem.GetQuantityInTransaction();
                float price = shopItem.GetPrice();

                for (int i = 0; i < quantity; i++) {

                    if (isBuyingMode) {
                        BuyItem(shopperInventory, shopperPurse, item, price);
                    } else {
                        SellItem(shopperInventory, shopperPurse, item, price);
                    }
                }
            }

            if (onChange != null) {
                onChange();
            }
        }

        private void SellItem(Inventory shopperInventory, Purse shopperPurse, InventoryItem item, float price) {
            int slot = FindFirstItemSlot(shopperInventory, item);
            if (slot == -1) return;

            AddToTransaction(item, -1);
            shopperInventory.RemoveFromSlot(slot, 1);
            if (!stockSold.ContainsKey(item)) {
                stockSold[item] = 0;
            }
            stockSold[item]--;
            shopperPurse.UpdateBalance(price);
        }

        private void BuyItem(Inventory shopperInventory, Purse shopperPurse, InventoryItem item, float price) {
            if (shopperPurse.GetBalance() < price) return;

            bool sucess = shopperInventory.AddToFirstEmptySlot(item, 1);
            if (sucess) {
                AddToTransaction(item, -1);
                if (!stockSold.ContainsKey(item)) {
                    stockSold[item] = 0;
                }
                stockSold[item]++;
                shopperPurse.UpdateBalance(-price);
            }
        }

        private int FindFirstItemSlot(Inventory shopperInventory, InventoryItem item) {
            for (int i = 0; i < shopperInventory.GetSize(); i++) {
                if (shopperInventory.GetItemInSlot(i) == item) {
                    return i;
                }
            }

            return -1;
        }

        public float TransactionTotal() {
            float total = 0;
            foreach (ShopItem item in GetAllItems()) {
                total += item.GetPrice() * item.GetQuantityInTransaction();
            }
            return total;
        }

        public void AddToTransaction(InventoryItem item, int quantity) {
            if (!transcation.ContainsKey(item)) {
                transcation[item] = 0;
            }

            var availabilities = GetAvailabilities();
            int availability = availabilities[item];
            if (transcation[item] + quantity > availability) {
                transcation[item] = availability;
            } else {
                transcation[item] += quantity;
            }
            if (transcation[item] <= 0) {
                transcation.Remove(item);
            }

            if (onChange != null) {
                onChange();
            }
        }

        public CursorType GetCursorType() {
            return CursorType.Shop;
        }

        public bool HandleRaycast(PlayerController callingController) {
            if (Input.GetMouseButtonDown(0)) {
                callingController.GetComponent<Shopper>().SetActiveShop(this);
            }
            return true;
        }

        public string GetShopName() {
            return shopName;
        }

        private int CountItemInInventory(InventoryItem item) {
            Inventory inventory = currentShopper.GetComponent<Inventory>();
            if (inventory == null) return 0;

            int total = 0;
            for (int i = 0; i < inventory.GetSize(); i++) {
                if (inventory.GetItemInSlot(i) == item) {
                    total += inventory.GetNumberInSlot(i);
                }
            }
            return total;
        }

        private int GetShopperLevel() {
            BaseStats stats = currentShopper.GetComponent<BaseStats>();
            if (stats == null) return 0;

            return stats.GetLevel();
        }

        private Dictionary<InventoryItem, float> GetPrices() {
            Dictionary<InventoryItem, float> prices = new Dictionary<InventoryItem, float>();

            foreach (var config in GetAvailableConfigs()) {
                if (isBuyingMode) {
                    if (!prices.ContainsKey(config.item)) {
                        prices[config.item] = config.item.GetPrice();
                    }

                    prices[config.item] *= (1 - config.buyingDiscountPercentage / 100f);
                } else {
                    prices[config.item] = config.item.GetPrice() * (sellingPercentage / 100);
                }
            }
            return prices;
        }

        private Dictionary<InventoryItem, int> GetAvailabilities() {
            Dictionary<InventoryItem, int> availabilities = new Dictionary<InventoryItem, int>();

            foreach (var config in GetAvailableConfigs()) {
                if (isBuyingMode) {
                    if (!availabilities.ContainsKey(config.item)) {
                        int sold = 0;
                        stockSold.TryGetValue(config.item, out sold);
                        availabilities[config.item] = -sold;
                    }
                    availabilities[config.item] += config.initialStock;
                } else {
                    availabilities[config.item] = CountItemInInventory(config.item);
                }
            }

            return availabilities;
        }

        private IEnumerable<StockItemConfig> GetAvailableConfigs() {
            int shopperLevel = GetShopperLevel();
            foreach (var config in stockConfig) {
                if (config.levelToUnlock > shopperLevel) continue;
                yield return config;
            }
        }

        public object CaptureState() {
            Dictionary<string, int> saveObject = new Dictionary<string, int>();
            foreach (var pair in stockSold) {
                saveObject[pair.Key.GetItemID()] = pair.Value;
            }

            return saveObject;
        }

        public void RestoreState(object state) {
            Dictionary<string, int> saveObject = (Dictionary<string, int>) state;
            stockSold.Clear();
            foreach (var pair in saveObject) {
                stockSold[InventoryItem.GetFromID(pair.Key)] = pair.Value;
            }
        }
    }
}
