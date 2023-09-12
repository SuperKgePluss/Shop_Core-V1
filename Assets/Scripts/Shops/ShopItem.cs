using GameDevTV.Inventories;
using System;
using UnityEngine;

namespace RPG.Shops {
    public class ShopItem {
        InventoryItem item;
        int availiability;
        float price;
        int quantityInTransaction;

        public ShopItem(InventoryItem item, int availability, float price, int quantityInTransaction) {
            this.item = item;
            this.availiability = availability;
            this.price = price;
            this.quantityInTransaction = quantityInTransaction;
        }

        public string GetName() {
            return item.GetDisplayName();
        }

        public Sprite GetIcon() {
            return item.GetIcon();
        }

        public int GetAvailability() {
            return availiability;
        }

        public float GetPrice() {
            return price;
        }

        internal InventoryItem GetInventoryItem() {
            return item;
        }
    }
}