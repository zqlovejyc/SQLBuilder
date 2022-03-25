#region License
/***
 * Copyright © 2018-2025, 张强 (943620963@qq.com).
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * without warranties or conditions of any kind, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion

using System;

namespace SQLBuilder.Diagnostics.Diagnostics
{
    /// <summary>
    /// IObserver泛型实现类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SqlBuilderObserver<T> : IObserver<T>
    {
        private readonly Action<T> _next;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="next"></param>
        public SqlBuilderObserver(Action<T> next)
        {
            _next = next;
        }

        /// <summary>
        /// 完成
        /// </summary>
        public void OnCompleted()
        {
        }

        /// <summary>
        /// 出错
        /// </summary>
        /// <param name="error"></param>
        public void OnError(Exception error)
        {
        }

        /// <summary>
        /// 下一步
        /// </summary>
        /// <param name="value"></param>
        public void OnNext(T value) => _next(value);
    }
}
