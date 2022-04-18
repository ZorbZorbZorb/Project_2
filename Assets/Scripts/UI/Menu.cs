using Assets.Scripts.Customers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts {
    [Serializable]
    public class Menu {
        /// <summary>
        /// List containing all open menus.
        /// </summary>
        public static List<Menu> OpenMenus = new List<Menu>();
        /// <summary>
        /// List containing all buttons for this menu.
        /// </summary>
        public static List<MenuButton> Buttons = new List<MenuButton>();
        /// <summary>
        /// List containing all buttons for this menu that change interactability or in some way update.
        /// </summary>
        public List<MenuButton> UpdatingButtons = new List<MenuButton>();
        public static List<MenuButton> AllUpdatingButtons = new List<MenuButton>();
        /// <summary>
        /// Closes all opened menus and clears the opened menus list.
        /// </summary>
        public static void CloseAllOpenMenus() {
            int c = OpenMenus.Count();
            if (OpenMenus.Count > 0) {
                for ( int i = 0; i < c; i++ ) {
                    try {
                        OpenMenus[i].Close();
                    }
                    catch {

                    }
                }
                OpenMenus.Clear();
            }
        }

        /// <summary>
        /// The canvas is the menu.
        /// <para>The canvas is enabled or disabled to open and close the menu.</para>
        /// </summary>
        [SerializeField]
        public readonly Canvas canvas;
        /// <summary>
        /// Code to call when checking if the menu can be opened right now
        /// <para>Accepts any function that returns bool</para>
        /// </summary>
        public Func<bool> canOpenNow;
        /// <summary>
        /// Menu is open or closed. Cached instead of calculated to be quicker
        /// </summary>
        public bool Enabled;
        public void Open() {
            if ( !canOpenNow() ) {
                return;
            }
            // Close other open menus
            CloseAllOpenMenus();
            // Open this menu and track it as open
            Enabled = true;
            canvas.gameObject.SetActive(true);
            OpenMenus.Add(this);
        }
        public void Close() {
            OpenMenus.Remove(this);
            Enabled = false;
            canvas.gameObject.SetActive(false);
        }
        public void Toggle() {
            if ( Enabled ) {
                Close();
            }
            else {
                Open();
            }
        }
        public void Update() {
            // If the menu is open and can't be opened now, close it.
            if (Enabled) {
                if (!canOpenNow()) {
                    Close();
                    return;
                }
                UpdatingButtons.ForEach(x => x.Update());
            }
        }
        public void AddButton(MenuButton button) {
            Buttons.Add(button);
            if (button.Enabled != null || button.Interactable != null || button.Enabled != null) {
                UpdatingButtons.Add(button);
            }
        }
        public static void ClearForSceneReload() {
            OpenMenus.Clear();
            Buttons.Clear();
            AllUpdatingButtons.Clear();
        }
        public Menu(Canvas _canvas) : this(_canvas, () => { return true; }) { }
        public Menu(Canvas _canvas, Func<bool> canOpenNow) {
            canvas = _canvas;
            canvas.sortingLayerName = "UI";
            this.canOpenNow = canOpenNow;
            Enabled = false;
            canvas.gameObject.SetActive(false);
        }
    }
}
