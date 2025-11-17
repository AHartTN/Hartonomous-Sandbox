using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class TopicKeywords : ITopicKeywords
{
    public int KeywordId { get; set; }

    public string? TopicName { get; set; }

    public string? Keyword { get; set; }

    public double? Weight { get; set; }
}
