﻿using System;

namespace GridDomain.EventSourcing
{
    /// <summary>
    ///     Represents an event message.
    /// </summary>
    public interface ISourcedEvent
    {
        /// <summary>
        ///     Gets the identifier of the source originating the event.
        /// </summary>
        string SourceId { get; }
        DateTime CreatedTime { get; }
    }
}