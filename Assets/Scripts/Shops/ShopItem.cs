using GameDevTV.Inventories;

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
    }
}