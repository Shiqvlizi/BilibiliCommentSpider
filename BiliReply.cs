﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BilibiliCommentSpider
{
    public class BiliReply
    {
        public string VideoId { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Depth { get; set; }
    }
}
