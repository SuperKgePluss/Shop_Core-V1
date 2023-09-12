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

        public event Action onChange;

        public IEnumerable<ShopItem> GetFilteredItems() {
            foreach (StockItemConfig config in stockConfig) {
                float price = config.item.GetPrice() * (1 - config.buyingDiscountPercentage / 100f);
                yield return new ShopItem(config.item, config.initialStock, price, 0);
            }
        }
        public void SelectFilter(ItemCategory category) { }
        public ItemCategory GetFilter() { return ItemCategory.None; }
        public void SelectMode(bool isBuying) { }
        public bool IsBuyingMode() { return true; }
        public bool CanTransact() { return true; }
        public void ConfirmTransaction() { }
        public float TransactionTotal() { return 0; }
        public void AddToTransaction(InventoryItem item, int quantity) {
            print($"Added to transaction: {item.GetDisplayName()} x {quantity}");
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
