//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SIPSorcery.SIP.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SIPSorcery.SIP.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;?xml version=&quot;1.0&quot; encoding=&quot;UTF-8&quot;?&gt;
        ///&lt;xs:schema
        ///  targetNamespace=&quot;urn:ietf:params:xml:ns:dialog-info&quot;
        ///  xmlns:xs=&quot;http://www.w3.org/2001/XMLSchema&quot;
        ///  xmlns:tns=&quot;urn:ietf:params:xml:ns:dialog-info&quot;
        ///  elementFormDefault=&quot;qualified&quot;
        ///  attributeFormDefault=&quot;unqualified&quot;&gt;
        ///  &lt;!-- This import brings in the XML language attribute xml:lang--&gt;
        ///  &lt;xs:import namespace=&quot;http://www.w3.org/XML/1998/namespace&quot; schemaLocation=&quot;http://www.w3.org/2001/03/xml.xsd&quot;/&gt;
        ///  &lt;xs:element name=&quot;dialog-info&quot;&gt;
        ///    &lt;xs:complexType&gt;
        ///    [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string EventDialogSchema {
            get {
                return ResourceManager.GetString("EventDialogSchema", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;?xml version=&quot;1.0&quot; encoding=&quot;UTF-8&quot;?&gt;
        ///&lt;xs:schema targetNamespace=&quot;urn:ietf:params:xml:ns:pidf&quot;
        ///     xmlns:tns=&quot;urn:ietf:params:xml:ns:pidf&quot;
        ///     xmlns:xs=&quot;http://www.w3.org/2001/XMLSchema&quot;
        ///     elementFormDefault=&quot;qualified&quot;
        ///     attributeFormDefault=&quot;unqualified&quot;&gt;
        ///
        ///  &lt;!-- This import brings in the XML language attribute xml:lang--&gt;
        ///  &lt;xs:import namespace=&quot;http://www.w3.org/XML/1998/namespace&quot; schemaLocation=&quot;http://www.w3.org/2001/xml.xsd&quot;/&gt;
        ///
        ///  &lt;xs:element name=&quot;presence&quot;&gt;
        ///    &lt;xs:complexType&gt;
        ///      &lt;xs:s [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string PIDFSchema {
            get {
                return ResourceManager.GetString("PIDFSchema", resourceCulture);
            }
        }
    }
}
