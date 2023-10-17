using GameDevTV.Inventories;
using RPG.Control;
using RPG.Inventories;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Shops {
    public class Shop : MonoBehaviour, IRaycastable {
        [SerializeField] string shopName;
        [SerializeField, Range(0, 100)] float sellingPercentage = 80f;

        //Stock Config
        [SerializeField] StockItemConfig[] stockConfig;

        [Serializable]
        class StockItemConfig {
            public InventoryItem item;
            public int initialStock;
            [Range(0, 100)] public float buyingDiscountPercentage;
        }

        Dictionary<InventoryItem, int> transcation = new Dictionary<InventoryItem, int>();
        Dictionary<InventoryItem, int> stock = new Dictionary<InventoryItem, int>();
        Shopper currentShopper = null;
        bool isBuyingMode = true;

        public event Action onChange;

        void Awake() {
            foreach (StockItemConfig config in stockConfig) {
                stock[config.item] = config.initialStock;
            }
        }

        public void SetShopper(Shopper shopper) {
            this.currentShopper = shopper;
        }

        public IEnumerable<ShopItem> GetFilteredItems() {
            return GetAllItems();
        }

        public IEnumerable<ShopItem> GetAllItems() {
            foreach (StockItemConfig config in stockConfig) {
                float price = GetPrice(config);
                int quantityInTransaction = 0;
                transcation.TryGetValue(config.item, out quantityInTransaction);
                int availability = GetAvailability(config.item);
                yield return new ShopItem(config.item, availability, price, quantityInTransaction);
            }
        }

        public void SelectFilter(ItemCategory category) { }
        public ItemCategory GetFilter() { return ItemCategory.None; }

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
            Purse purse = currentShopper.GetComponent<Purse>();
            if (purse == null) return false;

            return purse.GetBalance() >= TransactionTotal();
        }

        public bool IsTransactionEmpty() {
            return transcation.Count == 0;
        }

        public bool HasInventorySpace() {
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
                    if (shopperPurse.GetBalance() < price) break;

                    bool sucess = shopperInventory.AddToFirstEmptySlot(item, 1);
                    if (sucess) {
                        AddToTransaction(item, -1);
                        stock[item]--;
                        shopperPurse.UpdateBalance(-price);
                    }
                }
            }

            if (onChange != null) {
                onChange();
            }
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

            int availability = GetAvailability(item);
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

        private int GetAvailability(InventoryItem item) {
            if (isBuyingMode) {
                return stock[item];
            }
            return CountItemInInventory(item);
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

        private float GetPrice(StockItemConfig config) {
            if (isBuyingMode) {
                return config.item.GetPrice() * (1 - config.buyingDiscountPercentage / 100f);
            }

            return config.item.GetPrice() * (sellingPercentage / 100f);
        }
    }
}
