#if UNITY_EDITOR
using NoodledEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UltEvents;
using UnityEditor;
using UnityEngine;
using static NoodledEvents.CookBook.NodeDef;


public class StaticMethodCookBook : CookBook
{
    public override string Name { get; } = "Static Methods";
    
    private Dictionary<MethodInfo, NodeDef> MyDefs = new();
    public override void CollectDefs(Action<IEnumerable<NodeDef>, float> progressCallback, Action completedCallback)
    {
        MyDefs.Clear();
        int i = 0;
        var p = Task.Run(() => Parallel.ForEach<Type>(UltNoodleEditor.SearchableTypes, (t) =>
        {
            try
            {
                List<NodeDef> newDefs = new();
                foreach (var meth in t.GetMethods(UltEventUtils.AnyAccessBindings))
                {
                    if (meth.DeclaringType != t) continue;
                    if (!meth.IsStatic) continue;

                    string searchText = $"static {t.GetFriendlyName()}.{meth.Name}";
                    string descriptiveText = $"static {meth.ReturnType.GetFriendlyName()} {t.Namespace}.{t.GetFriendlyName()}.{meth.Name}";

                    var parames = meth.GetParameters();
                    if (parames.Length == 0) descriptiveText += "()";
                    else
                    {
                        descriptiveText += "(";
                        searchText += "(";
                        foreach (var param in parames)
                        {
                            searchText += $"{param.ParameterType.GetFriendlyName()}, ";
                            descriptiveText += $"{param.ParameterType.GetFriendlyName()} {param.Name}, ";
                        }
                        descriptiveText = descriptiveText.Substring(0, descriptiveText.Length - 2);
                        searchText = searchText.Substring(0, searchText.Length - 2);
                        descriptiveText += ")";
                        searchText += ")";
                    }


                    var newDef = new NodeDef(this, t.GetFriendlyName() + "." + meth.Name,
                        inputs: () =>
                        {
                            var @params = meth.GetParameters();
                            if (@params == null || @params.Length == 0) return new Pin[] { new NodeDef.Pin("Exec") };
                            return @params.Select(p => new Pin(p.Name, p.ParameterType)).Prepend(new NodeDef.Pin("Exec")).ToArray();
                        },
                        outputs: () =>
                        {
                            if (meth.GetRetType() != typeof(void))
                                return new[] { new NodeDef.Pin("Done"), new NodeDef.Pin(meth.ReturnType.GetFriendlyName(), meth.ReturnType) };
                            else return new[] { new NodeDef.Pin("Done") };
                        },
                        bookTag: JsonUtility.ToJson(new SerializedMethod() { Method = meth }),
                        searchTextOverride: searchText,
                        tooltipOverride: descriptiveText);
                    newDefs.Add(newDef);
                    UltNoodleEditor.MainThread.Enqueue(() => MyDefs.Add(meth, newDef));
                }
                progressCallback.Invoke(newDefs, (++i / (float)UltNoodleEditor.SearchableTypes.Length));
            }
            catch (TypeLoadException) { }
        }));
        p.ContinueWith(t => completedCallback.Invoke());
    }
    
    public override void CompileNode(UltEventBase evt, SerializedNode node, Transform dataRoot)
    {
        base.CompileNode(evt, node, dataRoot);
        // figure node method
        SerializedMethod meth = JsonUtility.FromJson<SerializedMethod>(node.BookTag);

        // sanity check (AddComponent() leaves this field empty)
        if (evt.PersistentCallsList == null) evt.FSetPCalls(new());

        // foreach input
        
        PersistentCall myCall = new PersistentCall(); // make my PCall
        myCall.SetMethod(meth.Method, null);
        if (node.DataInputs.Length > 0)
            myCall.FSetArguments(new PersistentArgument[node.DataInputs.Length]);


        for (int j = 0; j < node.DataInputs.Length; j++) // foreach node input
        {

            NoodleDataInput @in = node.DataInputs[j];
            @in.CompEvt = evt;
            @in.CompCall = myCall;

            if (@in.Source != null) // is connected
            {
                new PendingConnection(@in.Source, evt, myCall, j).Connect(dataRoot);
            } else
            {
                @in.CompArg = myCall.PersistentArguments[j] = new PersistentArgument(meth.Parameters[j]);

                // this input uses a default value!
                PersistentArgumentType t = PersistentArgumentType.None;
                if (node.DataInputs[j].ConstInput != PersistentArgumentType.None) // takes consts!
                {
                    t = node.DataInputs[j].ConstInput;
                    // force this arg to be the const lol, ui todo
                    myCall.PersistentArguments[j].FSetType(t);
                }
                else t = myCall.PersistentArguments[j].Type;

                switch (t)
                {
                    case PersistentArgumentType.Bool:
                        myCall.PersistentArguments[j].Bool = node.DataInputs[j].DefaultBoolValue;
                        break;
                    case PersistentArgumentType.String:
                        myCall.PersistentArguments[j].String = node.DataInputs[j].DefaultStringValue;
                        break;
                    case PersistentArgumentType.Int:
                        myCall.PersistentArguments[j].Int = node.DataInputs[j].DefaultIntValue;
                        break;
                    case PersistentArgumentType.Enum:
                        myCall.PersistentArguments[j].Enum = node.DataInputs[j].DefaultIntValue;
                        break;
                    case PersistentArgumentType.Float:
                        myCall.PersistentArguments[j].Float = node.DataInputs[j].DefaultFloatValue;
                        break;
                    case PersistentArgumentType.Vector2:
                        myCall.PersistentArguments[j].Vector2 = node.DataInputs[j].DefaultVector2Value;
                        break;
                    case PersistentArgumentType.Vector3:
                        myCall.PersistentArguments[j].Vector3 = node.DataInputs[j].DefaultVector3Value;
                        break;
                    case PersistentArgumentType.Vector4:
                        myCall.PersistentArguments[j].Vector4 = node.DataInputs[j].DefaultVector4Value;
                        break;
                    case PersistentArgumentType.Quaternion:
                        myCall.PersistentArguments[j].Quaternion = node.DataInputs[j].DefaultQuaternionValue;
                        break;
                    case PersistentArgumentType.Color:
                        myCall.PersistentArguments[j].Color = node.DataInputs[j].DefaultColorValue;
                        break;
                    case PersistentArgumentType.Color32:
                        myCall.PersistentArguments[j].Color32 = node.DataInputs[j].DefaultColorValue; // hope this implicitly casts well
                        break;
                    case PersistentArgumentType.Rect:
                        Vector4 v = node.DataInputs[j].DefaultVector4Value;
                        myCall.PersistentArguments[j].Rect = new Rect(v.x, v.y, v.z, v.w);
                        break;
                    case PersistentArgumentType.Object:
                        myCall.PersistentArguments[j].Object = node.DataInputs[j].DefaultObject;
                        myCall.PersistentArguments[j].FSetString(node.DataInputs[j].Type.Type.AssemblyQualifiedName);
                        break;
                }
            }
        }
        
        if (evt.PersistentCallsList == null) evt.FSetPCalls(new List<PersistentCall>());
        evt.PersistentCallsList.Add(myCall);
        // calls have been added/linked;
        // set compcall and compile next node.
        if (node.DataOutputs.Length > 0)
        {
            node.DataOutputs[0].CompEvt = evt;
            node.DataOutputs[0].CompCall = myCall;
        }
        var nextNode = node.FlowOutputs[0].Target?.Node;
        if (nextNode != null)
            nextNode.Book.CompileNode(evt, nextNode, dataRoot);
    }

    public override Dictionary<string, NodeDef> GetAlternatives(SerializedNode node)
    {
        Type srcType = null;
        if (node.Book == this || node.Book.name == "ObjectMethodCookBook")
        {
            SerializedMethod meth = JsonUtility.FromJson<SerializedMethod>(node.BookTag);
            srcType = meth.Method.DeclaringType;
        }
        else if (node.Book.name == "ObjectFieldCookBook")
        {
            SerializedField srcField = JsonUtility.FromJson<SerializedField>(node.BookTag);
            srcType = srcField.Field.DeclaringType;
        }
        else return null;
        Dictionary<string, NodeDef> o = new();
        // figure node method

        List<Type> relatives = new List<Type> { srcType };
        Type cur = srcType;
        while (cur != typeof(object))
        {
            cur = cur.BaseType;
            relatives.Add(cur);
        }
        relatives.Add(cur);

        foreach (var t in relatives)
        {
            string tName = t.GetFriendlyName();
            PropertyInfo[] props = t.GetProperties(UltEventUtils.AnyAccessBindings).Where(p=> (p.CanRead && p.GetMethod.IsStatic) || (p.CanWrite && p.SetMethod.IsStatic)).ToArray();
            MethodInfo[] meths = t.GetMethods(UltEventUtils.AnyAccessBindings).Where(m => m.IsStatic && !props.Any(p => p.GetMethod == m || p.SetMethod == m)).ToArray();
            
            foreach (var method in meths.Distinct())
            {
                if (!MyDefs.TryGetValue(method, out NodeDef def)) continue;

                // lets collapse each overload into a submenu!
                // also collapse "Injected" methods
                if (meths.Any(m => m.Name == method.Name && m != method))
                {
                    o.TryAdd(tName + "/Static/Methods/" + method.ReturnType.GetFriendlyName() + " " + def.SearchItem.text.Split('(').First() + "(...)/("
                        + string.Join(", ", method.GetParameters().Select(p => p.ParameterType.GetFriendlyName() + " " + p.Name)) + ")", def);
                }
                else
                    o.TryAdd(tName + "/Static/Methods/" + method.ReturnType.GetFriendlyName() + " " + def.SearchItem.text, def);
            }
            foreach (var prop in props)
            {
                if (prop.CanRead && MyDefs.TryGetValue(prop.GetMethod, out NodeDef getter))
                {
                    o.TryAdd(tName + "/Static/Properties/" + getter.SearchItem.text.Replace(".get_", ".").Split('(').First() + "/Get", getter);
                }
                if (prop.CanWrite && MyDefs.TryGetValue(prop.SetMethod, out NodeDef setter))
                {
                    o.TryAdd(tName + "/Static/Properties/" + setter.SearchItem.text.Replace(".set_", ".").Split('(').First() + "/Set", setter);
                }
            }
        }
        return o;
    }
}
#endif
