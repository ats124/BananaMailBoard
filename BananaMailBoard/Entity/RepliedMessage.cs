using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SQLite;

namespace BananaMailBoard.Entity
{
    /// <summary>
    /// �ԐM�ς݃��b�Z�[�W
    /// </summary>
    [Table("RepliedMessages")]
    public class RepliedMessage
    {
        /// <summary>
        /// ���b�Z�[�WID�B
        /// </summary>
        [PrimaryKey]
        public string MessageUid { get; set; }
    }
}