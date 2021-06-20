using System;
using UnityEngine;
using Raidflux;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MothershipButton : MonoBehaviour
{
    private Mothership mothership;
    private Action<Mothership> onClicked;
    private Button button;
    public Text buttonText;
    
    private void Awake()
    {
        button = GetComponent<Button>();
    }

    void Start()
    {
        button.onClick.AddListener(() =>
        {
            this.onClicked.Invoke(this.mothership);
        });
    }

    public void SetData(Mothership mothership, Action<Mothership> onClicked)
    {
        this.mothership = mothership;
        this.onClicked = onClicked;
        buttonText.text = mothership.region;
    }
}
