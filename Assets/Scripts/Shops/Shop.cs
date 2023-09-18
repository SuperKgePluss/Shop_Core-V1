using GameDevTV.Inventories;
using RPG.Control;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Shops {
    public class Shop : MonoBehaviour, IRaycastable {
        [SerializeField] string shopName;

        //Stock Config
        [SerializeField] StockItemConfig[] stockConfig;

        [Serializable]
        class StockItemConfig {
            public InventoryItem item;
            public int initialStock;
            [Range(0, 100)] public float buyingDiscountPercentage;
        }

        Dictionary<InventoryItem, int> transcation = new Dictionary<InventoryItem, int>();
        Shopper currentShopper = null;

        public event Action onChange;

        public void SetShopper(Shopper shopper) {
            this.currentShopper = shopper;
        }

        public IEnumerable<ShopItem> GetFilteredItems() {
            return GetAllItems();
        }

        public IEnumerable<ShopItem> GetAllItems() {
            foreach (StockItemConfig config in stockConfig) {
                float price = config.item.GetPrice() * (1 - config.buyingDiscountPercentage / 100f);
                int quantityInTransaction = 0;
                transcation.TryGetValue(config.item, out quantityInTransaction);
                yield return new ShopItem(config.item, config.initialStock, price, quantityInTransaction);
            }
        }

        public void SelectFilter(ItemCategory category) { }
        public ItemCategory GetFilter() { return ItemCategory.None; }
        public void SelectMode(bool isBuying) { }
        public bool IsBuyingMode() { return true; }
        public bool CanTransact() { return true; }
        public void ConfirmTransaction() {
            Inventory shopperInventory = currentShopper.GetComponent<Inventory>();
            if (shopperInventory == null) return;

            var transactionSnapshot = new Dictionary<InventoryItem, int>(transcation);
            foreach (InventoryItem item in transactionSnapshot.Keys) {
                int quantity = transactionSnapshot[item];
                for (int i = 0; i < quantity; i++) {
                    bool sucess = shopperInventory.AddToFirstEmptySlot(item, 1);
                    if (sucess) {
                        AddToTransaction(item, -1);
                    }
                }
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
            transcation[item] += quantity;
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
    }
}
