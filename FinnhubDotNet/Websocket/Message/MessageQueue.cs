namespace FinnhubDotNet.Websocket.Message {
    internal class MessageQueue {
        private readonly Queue<BaseMessage> msgQueue = new Queue<BaseMessage>();
        private readonly object lockObj = new object();

        public int count {
            get {
                lock (lockObj) {
                    return msgQueue.Count;
                }
            }
        }

        public void Enqueue(BaseMessage msg) {
            lock (lockObj) {
                msgQueue.Enqueue(msg);
            }
        }

        public BaseMessage Dequeue() {
            lock (lockObj) {
                return msgQueue.Dequeue();
            }
        }

        public bool TryDequeue(out BaseMessage msg) {
            lock (lockObj) {
                return msgQueue.TryDequeue(out msg);
            }
        }
    }
}
