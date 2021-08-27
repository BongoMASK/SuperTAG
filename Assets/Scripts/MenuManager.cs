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
    [SerializeField] Menu[] tipMenus;

    private void Awake() {
        Instance = this;
        tipText.text = gameTips.GetRandomTip();
    }

    public void OpenMenu(string menuName) {
<<<<<<< HEAD
        for (int i = 0; i < menus.Length; i++) {
            if (menus[i].menuName == menuName) {
=======
        if(menuName == "loading")
            tipText.text = gameTips.GetRandomTip();

        for(int i = 0; i < menus.Length; i++) {
            if(menus[i].menuName == menuName) {
>>>>>>> parent of 02ab150 (update to tips)
                menus[i].Open();
            }
            else if(menus[i].open) {
                CloseMenu(menus[i]);
            }
        }

<<<<<<< HEAD
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

=======
>>>>>>> parent of 02ab150 (update to tips)
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

    public void CloseMenu(Menu menu) {
        menu.Close();
    }

    public void CloseAllMenus() {
        foreach (Menu menu in menus)
            CloseMenu(menu);
    }

    bool CheckIfTipScreen(Menu menu) {
        for (int i = 0; i < tipMenus.Length; i++)
            if (menu == tipMenus[i])
                return true;

        return false;
    }
}
