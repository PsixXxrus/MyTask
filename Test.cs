Info02.12.2025 08:15:06.291WS55784u3750srkbr-rcvЗавершение обработки файла 414d512046524f4e5447415445202020692b364823dc96aa
Warning02.12.2025 08:16:46.388WS55784u3750srkbr - http.getDataSystem.Threading.Tasks.TaskCanceledException: Отменена задача.
   в System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   в System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   в kbr.Transport.Http.HttpConnection.<GetResponseAsync>d__14.MoveNext()
--- Конец трассировка стека из предыдущего расположения, где возникло исключение ---
   в System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
   в System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   в System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   в kbr.Transport.Http.HttpRequestHandler.<ReadAsync>d__10.MoveNext()
--- Конец трассировка стека из предыдущего расположения, где возникло исключение ---
   в System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
   в System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   в System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   в kbr.Transport.Http.HttpRequestHandler.Read(String itemId)
   в kbr.Transport.Http.SvkHttpReader.Read(SourceItem item, GetMessageDataResult result)
   в kbr.Transport.Http.BufferedSvkHttpInput.<>c__DisplayClass6_0.<getData>b__0(Boolean isLastAttempt)
   в kbr.Transport.ASvkSource.doQuery(String query_method_name, QueryMethod query_method)
Info02.12.2025 08:16:55.212WS55784u3750srОкно протоколаОкно протокола закрыто
Info02.12.2025 08:16:59.262WS55784u3750srШлюз.Оператор вызвал перезапуск обработчиков всех точек обмена.
Info02.12.2025 08:16:59.262WS55784u3750srkbr-rcvОжидаем остановку потока
Info02.12.2025 08:17:07.374WS55784u3750srОкно протоколаОкно протокола открыто
Info02.12.2025 08:17:10.395WS55784u3750srkbr-rcvВызов GateHandlerThread.Abort()
Warning02.12.2025 08:17:10.395WS55784u3750srkbr - http.getDataSystem.Threading.ThreadAbortException: Поток находился в процессе прерывания.
   в System.Threading.Monitor.ObjWait(Boolean exitContext, Int32 millisecondsTimeout, Object obj)
   в System.Threading.ManualResetEventSlim.Wait(Int32 millisecondsTimeout, CancellationToken cancellationToken)
   в System.Threading.Tasks.Task.SpinThenBlockingWait(Int32 millisecondsTimeout, CancellationToken cancellationToken)
   в System.Threading.Tasks.Task.InternalWait(Int32 millisecondsTimeout, CancellationToken cancellationToken)
   в System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   в kbr.Transport.Http.HttpRequestHandler.Read(String itemId)
   в kbr.Transport.Http.SvkHttpReader.Read(SourceItem item, GetMessageDataResult result)
   в kbr.Transport.Http.BufferedSvkHttpInput.<>c__DisplayClass6_0.<getData>b__0(Boolean isLastAttempt)
   в kbr.Transport.ASvkSource.doQuery(String query_method_name, QueryMethod query_method)
Error02.12.2025 08:17:10.582WS55784u3750srkbr-rcv414d512046524f4e5447415445202020692b364823dc96aa: System.Threading.ThreadAbortException: Поток находился в процессе прерывания.
   в kbr.Transport.ASvkSource.doQuery(String query_method_name, QueryMethod query_method)
   в kbr.Transport.Http.BufferedSvkHttpInput.getData(SourceItem item)
   в kbr.Transport.MessageBuffer.GetNextItem(GetMessageDataResult& result)
   в uarm.Gate.SAX.GateRcv.<processMessage>b__6_0(HistoryLevel cycle_history)
