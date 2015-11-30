#region Header
//---------------------------------------------------------------------------------------------------
// <copyright file="" company="Cognizant">
//		Copyright (c) Cognizant. All rights reserved.
// </copyright>
// <header>
//		<author>Ravi Shankar</author>
//		<description>
//          This class contains the fault message details
//		</description>
//		<changelog>
//	<date value=""></date>
//	</changelog>
// </header>
//---------------------------------------------------------------------------------------------------
#endregion


using System;
using System.Xml.Serialization;



namespace CGN.Paralegal.BusinessEntities
{
    //[DataContract(Namespace = "")]
    [Serializable]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [XmlTypeAttribute("fault")]
    [XmlRootAttribute(Namespace="http://services.paralegal.com/xmlschema/fault/1")]
    public class ErrorEntity
    {
        private Reason errorreason;
       //private Parameter parameter;
        [XmlElementAttribute("reason")]
        public Reason Reason
        {
            get
            {
                if(errorreason== null) 
                    return new Reason();
                else
                    return errorreason;
            }
            set {
                errorreason = value ?? new Reason();
            }
        }
    }

    [Serializable]
    public class Reason
    {
        private Parameter parameter;

        [XmlAttribute("code")]
        public string Code{get;set;}
        [XmlAttribute("correlationid")]
        public string CorrelationId{get;set;}
        [XmlAttribute("responsecode")]
        public string ResponseCode{get;set;}
        [XmlElementAttribute("message")]
        public string Message{get;set;}

        [XmlElementAttribute("parameter")]

        public Parameter Parameter
        {
            get
            {
                if (parameter == null)
                    return new Parameter();
                else
                    return parameter;
            }

            set { parameter = value; }
        }

    }


    [Serializable]
    public class Parameter
    {
        [XmlAttribute("style")]
        public string Style{get;set;}
        [XmlAttribute("name")]
        public string Name{get;set;}
    }



}
