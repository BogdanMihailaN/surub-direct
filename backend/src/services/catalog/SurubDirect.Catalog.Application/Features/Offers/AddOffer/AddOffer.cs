namespace SurubDirect.Catalog.Application;
public record AddOffer(string Code, string Label, int UnitsPerPack, int MinPacks = 1, int PackStep = 1);
