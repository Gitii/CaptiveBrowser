using System;

namespace Socks5;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ExtensionAttribute : Attribute { }
