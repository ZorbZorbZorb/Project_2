using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Assets.Scripts {
    [Serializable]
    public class MenuButton {
        public Customer Customer;
        public Menu Menu;
        public Button Button;
        public delegate bool Interactable(Customer customer);
        public Interactable interactable;
        public void Update() {
            if ( interactable != null) {
                Button.interactable = interactable(Customer);
            }
        }
        /// <summary>
        /// Creates a new button for a menu.
        /// <para>Automatically adds the button to the menu.</para>
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="onClick"></param>
        /// <param name="interactable"></param>
        public MenuButton(Customer customer, Menu menu, Button button, UnityAction onClick, Interactable _interactable = null) {
            Customer = customer;
            Button = button;
            Button.onClick.AddListener(onClick);
            interactable = _interactable;
            menu.AddButton(this);
        }
    }
}
