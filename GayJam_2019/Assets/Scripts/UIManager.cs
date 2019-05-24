using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Slider energyBar;
    public Transform patyk20;
    private bool p20 = false;
    public Transform patyk40;
    private bool p40 = false;
    public Transform patyk60;
    private bool p60 = false;
    public Transform patyk80;
    private bool p80 = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //AKTYWACJA

        if (!p20 && energyBar.value>=20)
        {
            p20 = true;
           //animation p20 play  
        }

        if (!p40 && energyBar.value >= 40)
        {
            p40 = true;
            //animation p40 play
        }

        if (!p60 && energyBar.value >= 60)
        {
            p60 = true;
            //animation p60 play
        }

        if (!p80 && energyBar.value >= 80)
        {
            p80 = true;
            //animation p80 play
        }

        //DEZAKTYWACJA

        if (p20 && energyBar.value <= 20)
        {
            p20 = false;
            //animation p20 play  
        }

        if (p40 && energyBar.value <= 40)
        {
            p40 = false;
            //animation p40 play
        }

        if (p60 && energyBar.value <= 60)
        {
            p60 = false;
            //animation p60 play
        }

        if (p80 && energyBar.value <= 80)
        {
            p80 = false;
            //animation p80 play
        }
    }
}
