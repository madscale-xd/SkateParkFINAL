using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkateSelectionView :  View
{
    [SerializeField] private InstantiatePlayer ins;
    public override void Initialize()
    {
        
    }

        void Start()
    {
         Cursor.visible = true;
         Time.timeScale = 0f;
    }

        void Update()
    {

    }

    public void SpawnLeon(){
        ins.SpawnPlayer(0,"skateboard");
        ViewManager.Show<UIView>();
        Time.timeScale = 1f;
        Cursor.visible = false;
    }

     public void SpawnClaire(){
        ins.SpawnPlayer(1,"skateboard");
        ViewManager.Show<UIView>();
        Time.timeScale = 1f;
        Cursor.visible = false;
    }
}
