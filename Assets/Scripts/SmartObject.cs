using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SmartObject
{
    [field: SerializeField]
    public string Name { get; private set; }

    [field: SerializeField]
    public string Type { get; private set; }

    [field: SerializeField]
    public string ID { get; private set; }

    [field: SerializeField]
    public string Description { get; private set; }

    [field: SerializeField]
    public List<Property> Properties { get; private set; } = new List<Property>();

    [field: SerializeField]
    public List<Variable> Variables { get; private set; } = new List<Variable>();

    [field: SerializeField]
    public List<Action> Actions { get; private set; } = new List<Action>();

    [Serializable]
    public class Property
    {
        [field: SerializeField]
        public string Name { get; private set; }

        [field: SerializeField]
        public string Value { get; private set; }

        [field: SerializeField]
        public string Type { get; private set; }
    }

    [Serializable]
    public class Variable
    {
        [field: SerializeField]
        public string Name { get; private set; }

        [field: SerializeField]
        public string Value { get; private set; }

        [field: SerializeField]
        public string Type { get; private set; }
    }

    [Serializable]
    public class Action
    {
        [field: SerializeField]
        public string Name { get; private set; }

        [field: SerializeField]
        public string ID { get; private set; }

        [field: SerializeField]
        public List<Parameter> Parameters { get; private set; } = new List<Parameter>();

        [Serializable]
        public class Parameter
        {
            [field: SerializeField]
            public string Name { get; private set; }

            [field: SerializeField]
            public string Type { get; private set; }

            [field: SerializeField]
            public bool Required { get; private set; }

            [field: SerializeField]
            public List<string> AllowedValues { get; private set; } = new List<string>();

            [field: SerializeField]
            public float? MinValue { get; private set; }

            [field: SerializeField]
            public float? MaxValue { get; private set; }
        }
    }
}