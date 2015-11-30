using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace CGN.Paralegal.Infrastructure
{
    //--------------------------------------------------------------------------------
    static public class EvEventHelper
    {
        static Dictionary<Type, List<FieldInfo>> EventFieldInfos = new Dictionary<Type, List<FieldInfo>>();

        static BindingFlags AllBindings
        {
            get { return BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static; }
        }


        static List<FieldInfo> GetTypeEventFields(Type t)
        {
            if (EventFieldInfos.ContainsKey(t))
                return EventFieldInfos[t];

            List<FieldInfo> fieldInfos = new List<FieldInfo>();
            BuildEventFields(t, fieldInfos);
            EventFieldInfos.Add(t, fieldInfos);
            return fieldInfos;
        }

        static void BuildEventFields(Type t, List<FieldInfo> lst)
        {
            // Type.GetEvent(s) gets all Events for the type AND it's ancestors
            // Type.GetField(s) gets only Fields for the exact type.
            //  (BindingFlags.FlattenHierarchy only works on PROTECTED & PUBLIC
            //   doesn't work because Fields are PRIVATE)

            // NEW version of this routine uses .GetEvents and then uses .DeclaringType
            // to get the correct ancestor type so that we can get the FieldInfo.
            lst.AddRange(from ei in t.GetEvents(AllBindings) let dt = ei.DeclaringType select dt.GetField(ei.Name, AllBindings) into fi where fi != null select fi);

        }

        //--------------------------------------------------------------------------------
        static EventHandlerList GetStaticEventHandlerList(Type t, object obj)
        {
            MethodInfo mi = t.GetMethod("get_Events", AllBindings);
            return (EventHandlerList)mi.Invoke(obj, new object[] { });
        }

        //--------------------------------------------------------------------------------

        //--------------------------------------------------------------------------------
        public static void RemoveEventHandler(object obj, string EventName)
        {
            if (obj == null)
                return;

            Type t = obj.GetType();
            List<FieldInfo> event_fields = GetTypeEventFields(t);
            EventHandlerList static_event_handlers = null;

            if (event_fields != null)
                foreach (FieldInfo fi in event_fields.Where(fi => EventName == "" || string.Compare(EventName, fi.Name, true) == 0))
                {
                    // After hours and hours of research and trial and error, it turns out that
                    // STATIC Events have to be treated differently from INSTANCE Events...
                    if (fi.IsStatic)
                    {
                        // STATIC EVENT
                        if (static_event_handlers == null)
                            static_event_handlers = GetStaticEventHandlerList(t, obj);

                        object idx = fi.GetValue(obj);
                        Delegate eh = static_event_handlers[idx];
                        if (eh == null)
                            continue;

                        Delegate[] dels = eh.GetInvocationList();
                        if (dels == null)
                            continue;

                        EventInfo ei = t.GetEvent(fi.Name, AllBindings);
                        foreach (Delegate del in dels)
                            ei.RemoveEventHandler(obj, del);
                    }
                    else
                    {
                        // INSTANCE EVENT
                        EventInfo ei = t.GetEvent(fi.Name, AllBindings);
                        if (ei != null)
                        {
                            object val = fi.GetValue(obj);
                            Delegate mdel = (val as Delegate);
                            if (mdel != null)
                            {
                                foreach (Delegate del in mdel.GetInvocationList())
                                    ei.RemoveEventHandler(obj, del);
                            }
                        }
                    }
                }
        }

        //--------------------------------------------------------------------------------
    }
}
