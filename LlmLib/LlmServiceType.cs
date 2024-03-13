using System;
using System.Collections.Generic;
using System.Text;

namespace LuToolbox.Models
{
    public enum LlmServiceType
    {
        // Underlying service uses the legacy completion endpoint of LLM API
        LlmApi_Completion = 0,

        // Underlying service uses the chat endpoint of LLM API
        LlmApi_Chat = 1,

        // Underlying service uses the chat endpoint (OpenAI schema) of custom API
        CustomApi_Chat = 2,

        // Underlying service uses the completion endpoint (OpenAI schema) of custom API
        CustomApi_Completion = 3,
    }
}
