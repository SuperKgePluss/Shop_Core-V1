using RPG.Shops;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.UI.Shops {
    public class ShopUI : MonoBehaviour {
        Shopper shopper = null;
        Shop currentShop = null;

        private void Start() {
            shopper = GameObject.FindGameObjectWithTag("Player").GetComponent<Shopper>();
            if (shopper == null) return;

            shopper.activeShopChange += ShopChanged;

            ShopChanged();
        }

        void ShopChanged() {
            currentShop = shopper.GetActiveShop();
            gameObject.SetActive(currentShop != null);
        }
    }
}
