using System.Diagnostics.CodeAnalysis;
using Credfeto.Enumeration.Source.Generation.Attributes;
using NuGet.Common;

namespace Credfeto.Package.Push;

[EnumText(typeof(LogLevel))]
[SuppressMessage(category: "ReSharper", checkId: "PartialTypeWithSinglePart", Justification = "Needed for generated code")]
internal static partial class EnumExtensions
{
}