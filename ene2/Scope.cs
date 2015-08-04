using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ene2
{
    public class NamespaceManager
    {
        private List<String> actualNamespace;
        private Tree tree;
        public Int32 ebpOffset
        { get { return tree[actualNamespace].ebpOffset; } set { tree[actualNamespace].ebpOffset = value; } }

        public NamespaceManager(String rootNamespace)
        {
            actualNamespace = new List<String>();
            actualNamespace.Add(rootNamespace);
            tree = new Tree(rootNamespace);
        }

        public NamespaceIdentNode getNamespaceAbsolute()    //with global::
        {
            return new NamespaceIdentNode(actualNamespace);
        }

        public NamespaceIdentNode getNamespaceRelative()    //without global::
        {
            return new NamespaceIdentNode(actualNamespace.Skip(1).ToList());
        }

        /// <summary>
        /// Removes the global namespace.
        /// </summary>
        /// <param name="symbol">Symbol.</param>
        public void removeGlobalNamespace(ref IdentNode symbol)
        {
            if (symbol.hasNamespace && symbol.namespace_.v[0] == actualNamespace[0])
                symbol.namespace_.v = symbol.namespace_.v.Skip(1).ToList();
        }

        public void push(Int32 bytes)
        {
            tree[actualNamespace].push(bytes);
        }

        public void pop(Int32 bytes)
        {
            tree[actualNamespace].pop(bytes);
        }

        public void enterScope()
        {
            tree[actualNamespace].enterScope();
        }

        public void leaveScope()
        {
            tree[actualNamespace].leaveScope();
        }

        public void enterOrCreateNamespace(NamespaceIdentNode namespace_)
        {
            Tree actualTree = tree[actualNamespace];
            Tree tmp = actualTree[namespace_.v];

            actualNamespace.AddRange(namespace_.v);

            if (tmp == null)    //have to create that namespace
            {
                foreach (String name in namespace_.v)
                {                    
                    actualTree.addNamespace(name);
                    actualTree = actualTree[name];
                }
            }
        }

        public void ensureCreateNamespace(NamespaceIdentNode namespace_)
        {
            Tree actualTree = tree[actualNamespace];
            Tree tmp = actualTree[namespace_.v];

            if (tmp == null)    //have to create that namespace
            {
                foreach (String name in namespace_.v)
                {                    
                    actualTree.addNamespace(name);
                    actualTree = actualTree[name];
                }
            }
        }

        public void leaveNamespace(Int32 layers)
        {
            if (layers != 0)
            while (layers != 0)
            {
                actualNamespace.RemoveAt(actualNamespace.Count -1);
                layers --;
            }
        }

        public void register(IType label, Boolean willUseItNow = false)
        {
            Tree found = null;

            if (label.name.hasNamespace)
                found = tree[label.name.namespace_.v];
            else
                found = tree[actualNamespace];

            if (found != null)
                found.register(label, willUseItNow);
            else
                new Error(Errors.NamespaceUnknown, label.name.namespace_);
        }

        public Boolean isRegistered(IdentNode label, Boolean willUseItNow = true)
        {
            Tree found = null;

            if (label.hasNamespace)
                found = tree[label.namespace_.v];
            else
                found = tree[actualNamespace];

            if (found != null)
                return found.isRegistered(label, willUseItNow);

            new Error(Errors.NamespaceUnknown, label.namespace_);
            return false;
        }

        public IType getObj(IdentNode label)
        {
            Tree found = null;

            if (label.hasNamespace)
                found = tree[label.namespace_.v];
            else
                found = tree[actualNamespace];                 

            IType tmp = found.getObj(label);
            if (found == null || tmp == null)
            {
                tmp = tree[actualNamespace[0]].getObj(label);   //lets also serch in the global namespace
                if (tmp == null)
                {
                    new Error(Errors.NamespaceUnknown, label.namespace_);
                    return null;
                }
                else
                    return tmp;
            }
            else
                return tmp;           
        }
    }

    public class Tree
    {
        private ScopeManager scopeManager;
        public String name { get; private set; }
        private List<Tree> namespaces;

        public Int32 ebpOffset
        { get { return scopeManager.ebpOffset; } set { scopeManager.ebpOffset = value; } }

        public Tree(String Name)
        {
            name = Name;
            namespaces = new List<Tree>();
            scopeManager = new ScopeManager();
        }

        public void push(Int32 bytes)
        {
            scopeManager.push(bytes);
        }

        public void pop(Int32 bytes)
        {
            scopeManager.pop(bytes);
        }

        public void enterScope()
        {
            scopeManager.enterScope();
        }

        public void leaveScope()
        {
            scopeManager.leaveScope();
        }

        public void addNamespace(String namespace_)
        {
            namespaces.Add(new Tree(namespace_));
        }

        public void register(IType symbol, Boolean withUsage = false)
        {
            scopeManager.register(symbol, withUsage);
        }

        public Boolean isRegistered(IdentNode label, Boolean willUseItNow = true)
        {
            return scopeManager.isRegistered(label, willUseItNow);
        }

        public IType getObj(IdentNode label)
        {
            return scopeManager.getObj(label);
        }

        private Tree findElem(IList<String> paths)
        {
            Tree tmp = this;
            Int32 i = 0;

            while ((tmp = tmp[paths[i++]]) != null && i < paths.Count);
            if (tmp != null)
                return tmp;

            //new Error(Errors.NamespaceUnknown, String.Join("#", paths));
            return null;
        }

        public Tree this[IList<String> paths]
        {
            get { return this.findElem(paths); }
        }

        public Tree this[String key]
        {
            get
            {
                if (this.name == key)
                    return this;

                if (namespaces.Any(e => e.name == key))
                    return namespaces.Find(e => e.name == key);

                //new Error(Errors.NamespaceUnknown, key);
                return null;
            }
        }
    }

    public class ScopeManager
    {
        public List<Scope> scopes;

        public Int32 ebpOffset
        { get { return scopes.Last().ebpOffset; } set { scopes.Last().ebpOffset = value; } }

        public ScopeManager()
        {
            scopes = new List<Scope>();
            this.enterScope();
        }

        ~ScopeManager()
        {
            this.leaveScope();
        }

        public void enterScope()
        {
            scopes.Add(new Scope());
        }

        public void leaveScope()
        {
            scopes.Remove(scopes.Last());
        }

        public void push(Int32 bytes)
        {
            scopes.Last().ebpOffset += bytes;
        }

        public void pop(Int32 bytes)
        {
            scopes.Last().ebpOffset -= bytes;
        }

        public void register(IType symbol, Boolean withUsage = false)
        {
            if (symbol == null)
                new Error(Errors.Internal, symbol.ToString() + " was null.");

            scopes.Last().register(symbol, withUsage);
        }

        public Boolean isRegistered(IdentNode label, Boolean willUseItNow = true)
        {
            for (int i = 0; i < scopes.Count; i++)
                if (scopes[i].isRegistered(label))
                    return true;

            return false;
        }

        public IType getObj(IdentNode label)
        {
            for (int i = 0; i < scopes.Count; i++)
                if (scopes[i].isRegistered(label))
                    return scopes[i].getObj(label);

            return null;
        }
    }

    public class Scope
    {
        public List<IType> registeredLabels;
        public Int32 ebpOffset = 0;

        public Scope()
        {
            registeredLabels = new List<IType>();
        }

        public void register(IType symbol, Boolean withUsage = false)
        {
            if (symbol is VariableNode && ((VariableNode)symbol).type.name.v != "ptr")
                symbol.type = (TypeNode)this.getObj(symbol.type.name);

            registeredLabels.Add(symbol);
        }

        public Boolean isRegistered(IdentNode label, Boolean willUseItNow = true)
        {
            return registeredLabels.Any(e => e.name.v == label.v);
        }

        public IType getObj(IdentNode label)
        {
            IType found = registeredLabels.Find(e => e.name.v == label.v);

            if (found == null)
                new Error(Errors.LabelUnknown, label);

            return found;
        }
    }
}