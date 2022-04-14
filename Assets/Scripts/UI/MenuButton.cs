using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine;
using Assets.Scripts.Customers;

namespace Assets.Scripts {
    [Serializable]
    public class MenuButton {
        public Customer Customer;
        [NonSerialized]
        public Menu Menu;
        public Button Button;
        public Func<Customer, bool> Enabled;
        public Func<Customer, bool> Interactable;

        private Color black = new Color(75, 20, 10, 70);
        private Color red = new Color(255f, 75f, 40f, 130);
        public ColorBlock Colors;

        public void Update() {
            if (Customer == null) {
                Debug.LogError("Customer was null for button press.");
                return;
            }

            bool enabled = Enabled == null ? false : Enabled(Customer);
            bool interactable = Interactable == null ? true : Interactable(Customer);
            //Colors.disabledColor = enabled ? red : black;
            //Button.colors = Colors;
            Button.interactable = enabled && interactable;
        }
        /// <summary>
        /// Creates a new button for a menu.
        /// <para>Automatically adds the button to the menu.</para>
        /// </summary>
        public MenuButton(Customer customer, Menu menu, Button button, UnityAction onClick) {
            Customer = customer;
            Button = button;
            Button.onClick.AddListener(onClick);
            Interactable = null;
            Enabled = null;
            Colors = button.colors;
            menu.AddButton(this);
        }
        /// <summary>
        /// Creates a new button for a menu.
        /// <para>Automatically adds the button to the menu.</para>
        /// </summary>
        public MenuButton(Customer customer, Menu menu, Button button, UnityAction onClick, Func<Customer, bool> interactable ) {
            Customer = customer;
            Button = button;
            Button.onClick.AddListener(onClick);
            Interactable = interactable;
            Enabled = null;
            Colors = button.colors;
            menu.AddButton(this);
        }
        /// <summary>
        /// Creates a new button for a menu.
        /// <para>Automatically adds the button to the menu.</para>
        /// </summary>
        public MenuButton(Customer customer, Menu menu, Button button, UnityAction onClick, Func<Customer, bool> interactable, Func<Customer, bool> enabled) {
            Customer = customer;
            Button = button;
            Button.onClick.AddListener(onClick);
            Interactable = interactable;
            Enabled = enabled;
            Colors = button.colors;
            menu.AddButton(this);
        }
    }
}
