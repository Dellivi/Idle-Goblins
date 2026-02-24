using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class CustomBar : MonoBehaviour
{
    public delegate void BarEvent();
    public event BarEvent OnBarFinish;
    public event BarEvent OnBarChanged;
    public event BarEvent OnBarStarted;

    public Image bar;
    
    private double current;
    private double maximum;

    public double Maximum { get => maximum; protected set => maximum = value; }
    public double Current { get => current; protected set => current = value; }

    public virtual void SetBar(double amount)
    {
        maximum = amount;
        Current = amount;
    }

    public virtual void SetBar(double _current, double _maximum)
    {
        Current = _current;
        Maximum = _maximum;


        bar.fillAmount = GetCurrentNormilized();
    }

    public virtual void AddFillCurrent(double value)
    {
        this.Current += value;


        if (OnBarChanged != null)
        {
            OnBarChanged.Invoke();
        }

        if (Current >= Maximum)
        {
            Current = Maximum;
        }

        //Dead
        if (Current <= 0)
        {
            Current = 0;

            if(OnBarFinish != null) OnBarFinish.Invoke(); 
        }

        bar.fillAmount = GetCurrentNormilized();
    }

    public virtual void ReduceFillCurrent(double value)
    {
        this.Current -= value;


        if (OnBarChanged != null)
        {
            OnBarChanged.Invoke();
        }

        if (Current >= Maximum)
        {
            Current = Maximum;
        }


        //Dead
        if (Current <= 0)
        {
            Current = 0;

            if (OnBarFinish != null) OnBarFinish.Invoke();
        }

        bar.fillAmount = GetCurrentNormilized();
    }

    public virtual float GetCurrentNormilized()
    {
        float norm = (float)Current / (float)Maximum;
        return norm;
    }
}
