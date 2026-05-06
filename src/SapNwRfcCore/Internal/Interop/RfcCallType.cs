using System.Diagnostics.CodeAnalysis;

namespace SapNwRfcCore.Internal.Interop;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Should match SAP SDK")]
[SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Should match SAP SDK")]
internal enum RfcCallType
{
    RFC_SYNCHRONOUS,
    RFC_TRANSACTIONAL,
    RFC_QUEUED,
    RFC_BACKGROUND_UNIT,
}
