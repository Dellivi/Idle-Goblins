using NUnit.Framework;
using System;
using UnityEngine;

public class LocationSystem : MonoBehaviour
{
    public LocationView locationView;
    public CustomBar bar;

    private LocationData locationData;

    private int currentChapterIndex = 1;
    private int currentValue = 0;
    private int maxValue = 0;

    public int CurrentChapterIndex { get => currentChapterIndex; private set => currentChapterIndex = value; }
    public int CurrentValue { get => currentValue; set => currentValue = value; }
    public int MaxValue { get => maxValue; set => maxValue = value; }
    public LocationData LocationData { get => locationData; protected set => locationData = value; }

    private void OnEnable()
    {
        ResourceManager.Instance.OnResourceChanged += Instance_OnResourceChanged;
    }

    private void OnDisable()
    {
        ResourceManager.Instance.OnResourceChanged -= Instance_OnResourceChanged;
    }

    private void Start()
    {
        locationView.Setup(this);
    }
    
    public void Initialize(LocationData data)
    {
        LocationData = data;
        //IncreaseNextChapterResource();

        currentValue = (int)ResourceManager.Instance.GetResource(LocationData.resource);
        MaxValue = LocationData.GetMaximumValue(CurrentChapterIndex);
        locationView.Setup(this);
        bar.SetBar(CurrentValue, MaxValue);

        ActivateNextChapterButton();
    }

    private void Instance_OnResourceChanged(ResourceData data, float newValue, float previousValue)
    {
        CurrentValue = (int)newValue;

        ActivateNextChapterButton();

        locationView.Initialize();
        bar.AddFillCurrent(newValue - previousValue);
    }

    private void ActivateNextChapterButton()
    {
        if (CurrentValue >= MaxValue && CheckNextChapter())
        {
            locationView.BtnNextChapter.gameObject.SetActive(true);
        }
        else
        {
            locationView.BtnNextChapter.gameObject.SetActive(false);
        }
    }

    public void NextChapter()
    {
        if(LocationData.maxChapterIndex > CurrentChapterIndex)
        {
            CurrentChapterIndex++;
            IncreaseNextChapterResource();
        }
    }

    public bool CheckNextChapter()
    {
        if (CurrentChapterIndex < LocationData.maxChapterIndex)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void IncreaseNextChapterResource()
    {
        CurrentValue = 0;
        MaxValue = LocationData.GetMaximumValue(CurrentChapterIndex);

        bar.SetBar(CurrentValue, MaxValue);
    }
}
