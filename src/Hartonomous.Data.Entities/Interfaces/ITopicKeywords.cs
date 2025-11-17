using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface ITopicKeywords
{
    int KeywordId { get; set; }
    string? TopicName { get; set; }
    string? Keyword { get; set; }
    double? Weight { get; set; }
}
