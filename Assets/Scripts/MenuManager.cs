using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MenuManager : MonoBehaviour
{
    public Tip gameTips;

    public static MenuManager Instance;

    [SerializeField] TMP_Text tipText;
    [SerializeField] Menu[] menus;

    private void Awake() {
        Instance = this;
        tipText.text = gameTips.GetRandomTip();
    }

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
    }

    public void OpenMenu(Menu menu) {
        for (int i = 0; i < menus.Length; i++) {
            if (menus[i].open) {
                CloseMenu(menus[i]);
            }
        }
        menu.Open();
    }

    public void CloseMenu(Menu menu) {
        menu.Close();
    }

    public void CloseAllMenus() {
        foreach (Menu menu in menus)
            CloseMenu(menu);
    }
}
