using UnityEngine;
using TMPro;

public class MenuManager : MonoBehaviour
{
    public Tip gameTips;

    public static MenuManager Instance;

    [SerializeField] TMP_Text tipText;
    [SerializeField] Menu[] menus;
    [SerializeField] Menu[] tipMenus;

    private void Awake() {
        Instance = this;
    }

    /// <summary>
    /// Opens menu using unique string
    /// </summary>
    /// <param name="menuName"></param>
    public void OpenMenu(string menuName) {
        if(menuName == "loading")
            tipText.text = gameTips.GetRandomTip();

        for(int i = 0; i < menus.Length; i++) {
            if(menus[i].menuName == menuName) {
                menus[i].Open();
            }
            else if(menus[i].open) {
                CloseMenu(menus[i]);
            }
        }

        if (tipText == null) 
            return;

        if (menuName == "loading" || menuName == "title") {
            tipText.gameObject.SetActive(true);
            tipText.text = gameTips.GetRandomTip();
        }

        else {
            tipText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Opens menu using Menu object
    /// </summary>
    /// <param name="menu"></param>
    public void OpenMenu(Menu menu) {
        for (int i = 0; i < menus.Length; i++) {
            if (menus[i].open) {
                CloseMenu(menus[i]);
            }
        }
        menu.Open();

        if (tipText == null)
            return;

        tipText.gameObject.SetActive(CheckIfTipScreen(menu));
        tipText.text = gameTips.GetRandomTip();
    }

    /// <summary>
    /// Closes menu using Menu object
    /// </summary>
    /// <param name="menu"></param>
    public void CloseMenu(Menu menu) {
        menu.Close();
    }

    /// <summary>
    /// Closes all menus
    /// </summary>
    public void CloseAllMenus() {
        foreach (Menu menu in menus)
            CloseMenu(menu);
    }

    /// <summary>
    /// checks if current menu is a tip menu
    /// </summary>
    /// <param name="menu"></param>
    /// <returns></returns>
    bool CheckIfTipScreen(Menu menu) {
        for (int i = 0; i < tipMenus.Length; i++)
            if (menu == tipMenus[i])
                return true;

        return false;
    }
}