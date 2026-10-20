using SurubDirect.Inventory.Domain;
namespace SurubDirect.Inventory.Application;

public interface IReceiptCorrections { Task<bool> Apply(ReceiptCorrection correction, CancellationToken ct); }
