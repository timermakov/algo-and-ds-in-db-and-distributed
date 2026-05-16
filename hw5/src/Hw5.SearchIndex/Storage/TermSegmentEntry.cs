namespace Hw5.SearchIndex.Storage;

public sealed record TermSegmentEntry(long Offset, int Length, int DocumentFrequency);
