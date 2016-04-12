using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SQLite;

namespace BananaMailBoard.Entity
{
    /// <summary>
    /// 返信済みメッセージ
    /// </summary>
    [Table("RepliedMessages")]
    public class RepliedMessage
    {
        /// <summary>
        /// メッセージID。
        /// </summary>
        [PrimaryKey]
        public string MessageUid { get; set; }
    }
}