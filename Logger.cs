public class Logger {

    // 用于存放写日志任务的队列
    private Queue<Action> _queue;

    // 用于写日志的线程
    private Thread _loggingThread;

    // 用于通知是否有新日志要写的“信号器”
    private ManualResetEvent _hasNew;

    // 构造函数，初始化。
    public Logger() {
        _queue = new Queue<Action>();
        _hasNew = new ManualResetEvent(false);

        _loggingThread = new Thread(Process);
        _loggingThread.IsBackground = true;
        _loggingThread.Start();
    }

    // 使用单例模式，保持一个Logger对象
    private static readonly Logger _logger = new Logger();
    private static Logger GetInstance() {
        /* 不安全代码
        lock (locker) {
            if (_logger == null) {
                _logger = new Logger();
            }
        }*/
        return _logger;
    }

    // 处理队列中的任务
    private void Process() {
        while (true) {
            // 等待接收信号，阻塞线程。
            _hasNew.WaitOne();

            // 接收到信号后，重置“信号器”，信号关闭。
            _hasNew.Reset(); 

            // 由于队列中的任务可能在极速地增加，这里等待是为了一次能处理更多的任务，减少对队列的频繁“进出”操作。
            Thread.Sleep(100);

            // 开始执行队列中的任务。
            // 由于执行过程中还可能会有新的任务，所以不能直接对原来的 _queue 进行操作，
            // 先将_queue中的任务复制一份后将其清空，然后对这份拷贝进行操作。

            Queue<Action> queueCopy;
            lock (_queue) {
                queueCopy = new Queue<Action>(_queue);
                _queue.Clear();
            }

            foreach (var action in queueCopy) {
                action();
            }
        }
    }

    private void WriteLog(string content) {
        lock (_queue) { // todo: 这里存在线程安全问题，可能会发生阻塞。
            // 将任务加到队列
            _queue.Enqueue(() => File.AppendAllText("log.txt", content));
        }

        // 打开“信号”
        _hasNew.Set();
    }

    // 公开一个Write方法供外部调用
    public static void Write(string content) {
        // WriteLog 方法只是向队列中添加任务，执行时间极短，所以使用Task.Run。
        Task.Run(() => GetInstance().WriteLog(content));
    }
}
