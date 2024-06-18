using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using UniRx;
using UnityEngine;
using UnityEngine.Events;
using static UnityEditorInternal.VersionControl.ListControl;

public class DetectableObject : MonoBehaviour
{
    [Serializable]
    public class Property
    {
        public Property(string name, string value)
        {
            Name = name;
            Value = value;
        }

        [field: SerializeField]
        public string Name { get; private set; }

        [field: SerializeField]
        public string Value { get; private set; }

        public override string ToString()
        {
            return $"'{Name}' = '{Value}'";
        }
    }

    [Serializable]
    public class Variable
    {
        public Variable(string name)
        {
            Name = name;
        }

        public Variable(string name, string value)
        {
            Name = name;
            Value = new ReactiveProperty<string>(value);
        }

        [field: SerializeField]
        public string Name { get; private set; }

        [field: SerializeField]
        public ReactiveProperty<string> Value { get; private set; } = new ReactiveProperty<string>();

        public override string ToString()
        {
            return $"'{Name}' = '{Value.Value}'";
        }
    }

    [Serializable]
    public class Action
    {
        public Action(string name)
        {
            Name = name;
            ID = UnityEngine.Random.Range(0, int.MaxValue).ToString();
        }

        [field: SerializeField]
        public string ID { get; set; }

        [field: SerializeField]
        public string Name { get; private set; }

        [field: SerializeField]
        public string Description { get; private set; }

        public Delegate Delegate { get; private set; }

        public void SetAction<T>(T method) where T : Delegate
        {
            Delegate = method;
        }

        public object TryInvokeAction(params object[] args)
        {
            object result = null;

            try
            {
                result = Delegate.DynamicInvoke(args);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex);
            }

            return result;
        }

        public override string ToString()
        {
            try
            {
                ParameterInfo[] infos = Delegate.Method.GetParameters();

                string description = $"'{Name}': {{ ID: '{ID}';";

                if (!string.IsNullOrWhiteSpace(Description))
                {
                    description += $" Description: '{Description}';";
                }

                if (infos.Length > 0)
                {
                    description += $" Parameters: ";
                }

                for (int i = 0; i < infos.Length; i++)
                {
                    description += $" '{infos[i].Name}'";
                    if (i != infos.Length - 1)
                        description += ",";
                    else
                        description += "; }";
                }

                return description;
            }
            catch
            {
                return "";
            }
        }
    }

    [field: SerializeField]
    public string ObjectType { get; private set; }

    [field: SerializeField]
    public string ID { get; private set; }

    [field: SerializeField]
    public string Name { get; private set; }

    [field: SerializeField]
    public string Description { get; private set; }

    [field: SerializeField]
    public List<Property> Properties { get; private set; } = new List<Property>();

    [field: SerializeField]
    public List<Variable> Variables { get; private set; } = new List<Variable>();

    [field: SerializeField]
    public List<Action> PossibleActions { get; private set; } = new List<Action>();

    //[field: SerializeField]
    //public SmartObject SmartObject { get; private set; } = new SmartObject();

    private void OnEnable()
    {
        ID = UnityEngine.Random.Range(0, int.MaxValue).ToString();

        foreach (var action in PossibleActions)
        {
            action.SetAction(new System.Action(() => { }));
            action.ID = UnityEngine.Random.Range(0, int.MaxValue).ToString();
        }
    }

    public string GetDescription()
    {
        string description = string.Empty;

        if (!string.IsNullOrWhiteSpace(Name))
            description += $"Object '{Name}' ({ObjectType}); ";
        else
            description += $"Object '{ObjectType}'; ";

        description += $"ID = '{ID}'; ";
        if (!string.IsNullOrWhiteSpace(Description)) description += $"Description = '{Description}'; ";

        if (Properties.Count > 0)
        {
            description += "Properties: {";

            for (int i = 0; i < Properties.Count; i++)
            {
                description += $" {Properties[i]}";
                if (i != Properties.Count - 1)
                    description += ",";
                else
                    description += " }; ";
            }
        }

        if (Variables.Count > 0)
        {
            description += "Variables: {";

            for (int i = 0; i < Variables.Count; i++)
            {
                description += $" {Variables[i]}";
                if (i != Variables.Count - 1)
                    description += ",";
                else
                    description += " }; ";
            }
        }
        //else
        //{
        //    description += "This object has no described properties. ";
        //}

        if (PossibleActions.Count > 0)
        {
            description += "Possible actions: { ";

            for (int i = 0; i < PossibleActions.Count; i++)
            {
                description += $"{PossibleActions[i]}";
                if (i != PossibleActions.Count - 1)
                    description += ", ";
                else
                    description += " }; ";
            }
        }
        //else
        //{
        //    description += "There are no actions available for this object.";
        //}

        return description;
    }

    public string GetShortDescription()
    {
        string description = string.Empty;

        if (!string.IsNullOrWhiteSpace(Name))
            description += $"Object '{Name}' ({ObjectType}); ";
        else
            description += $"Object '{ObjectType}'; ";

        if (!string.IsNullOrWhiteSpace(Description)) description += $"Description = '{Description}'; ";

        if (Properties.Count > 0)
        {
            description += "Properties: {";

            for (int i = 0; i < Properties.Count; i++)
            {
                description += $" {Properties[i]}";
                if (i != Properties.Count - 1)
                    description += ",";
                else
                    description += " }; ";
            }
        }

        return description;
    }
}
