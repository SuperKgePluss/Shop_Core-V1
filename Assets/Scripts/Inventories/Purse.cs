using UnityEngine;

namespace RPG.Inventories {
    public class Purse : MonoBehaviour {
        [SerializeField] float startingBalance = 400f;

        float balance = 0;

        void Awake() {
            balance = startingBalance;    
        }

        public float GetBalance() {
            return balance;
        }

        public void UpdateBalance(float amount) {
            balance += amount;
            print($"Balance: {balance}");
        }
    }
}