using System;
using System.Collections.Generic;

namespace Shared
{
    public class MessageDto
    {
        public MessageDto(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }
        public string Value { get; }
        public string Result { get; set; }
    }

    public class Job
    {
        public Job(IEnumerable<MessageDto> messages)
        {
            Messages = messages;
        }

        public string ResultMessage { get; set; }
        public bool Success { get; set; }
        public string Requestor { get; set; }
        public IEnumerable<MessageDto> Messages { get; }
    }
}
