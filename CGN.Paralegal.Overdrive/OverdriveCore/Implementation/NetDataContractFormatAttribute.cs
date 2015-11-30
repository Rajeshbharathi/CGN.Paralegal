using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Xml;
/*
public class NetDataContractFormatAttribute : Attribute, IOperationBehavior
{
    public void AddBindingParameters(OperationDescription description, BindingParameterCollection parameters)
    {
    }

    public void ApplyClientBehavior(OperationDescription description, ClientOperation proxy)
    {
        ReplaceDataContractSerializerOperationBehavior(description);
    }

    public void ApplyDispatchBehavior(OperationDescription description, DispatchOperation dispatch)
    {
        ReplaceDataContractSerializerOperationBehavior(description);
    }
    public void Validate(OperationDescription description)
    {
    }

    private static void ReplaceDataContractSerializerOperationBehavior(OperationDescription description)
    {
        DataContractSerializerOperationBehavior dcs = description.Behaviors.Find<DataContractSerializerOperationBehavior>();

        if (dcs != null)
            description.Behaviors.Remove(dcs);

        description.Behaviors.Add(new NetDataContractSerializerOperationBehavior(description));
    }

    public class NetDataContractSerializerOperationBehavior : DataContractSerializerOperationBehavior
    {
        private static NetDataContractSerializer serializer = new NetDataContractSerializer();

        public NetDataContractSerializerOperationBehavior(OperationDescription operationDescription) : base(operationDescription) { }

        public override XmlObjectSerializer CreateSerializer(Type type, string name, string ns, IList<Type> knownTypes)
        {
            return NetDataContractSerializerOperationBehavior.serializer;
        }

        public override XmlObjectSerializer CreateSerializer(Type type, XmlDictionaryString name, XmlDictionaryString ns, IList<Type> knownTypes)
        {
            return NetDataContractSerializerOperationBehavior.serializer;
        }
    }
}
*/
public class UseNetDataContractSerializerAttribute : Attribute, IOperationBehavior
{
    public void AddBindingParameters(OperationDescription description, BindingParameterCollection parameters)
    {
    }
 
    public void ApplyClientBehavior(OperationDescription description, ClientOperation proxy)
    {
        ReplaceDataContractSerializerOperationBehavior(description);
    }
 
    public void ApplyDispatchBehavior(OperationDescription description, DispatchOperation dispatch)
    {
        ReplaceDataContractSerializerOperationBehavior(description);
    }
 
    public void Validate(OperationDescription description)
    {
    }
 
    private static void ReplaceDataContractSerializerOperationBehavior(OperationDescription description)
    {
        DataContractSerializerOperationBehavior dcsOperationBehavior = description.Behaviors.Find<DataContractSerializerOperationBehavior>();
 
        if (dcsOperationBehavior != null)
        {
            description.Behaviors.Remove(dcsOperationBehavior);
            description.Behaviors.Add(new NetDataContractOperationBehavior(description));
        }
    }
}

public class NetDataContractOperationBehavior : DataContractSerializerOperationBehavior
{
    public NetDataContractOperationBehavior(OperationDescription operation) : base(operation)
    {
    }
 
    public NetDataContractOperationBehavior(OperationDescription operation, DataContractFormatAttribute dataContractFormatAttribute)
            : base(operation, dataContractFormatAttribute)
    {
    }
 
    public override XmlObjectSerializer CreateSerializer(Type type, string name, string ns, IList<Type> knownTypes)
    {
        return new NetDataContractSerializer(name, ns);
    }
 
    public override XmlObjectSerializer CreateSerializer(Type type, XmlDictionaryString name, XmlDictionaryString ns, IList<Type> knownTypes)
    {
        return new NetDataContractSerializer(name, ns);
    }
}
