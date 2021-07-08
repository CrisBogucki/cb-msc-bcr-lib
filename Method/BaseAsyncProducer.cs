using System;
using System.Collections.Concurrent;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Types;

namespace BaseAsyncServices.Method
{
    public class BaseAsyncProducer : IBaseAsyncProducer
    {
        private IBasicProperties _props;
        private readonly IModel _channel;

        public BaseAsyncProducer()
        {
            var connectionFactory = new ConnectionFactory
            {
                UserName = Tools.GetAppSettingsValueString("rabbit", "user"),
                Password = Tools.GetAppSettingsValueString("rabbit", "pass"),
                HostName = Tools.GetAppSettingsValueString("rabbit", "host"),
                Port = Protocols.DefaultProtocol.DefaultPort,
                VirtualHost = Tools.GetAppSettingsValueString("rabbit", "vhost"),
                DispatchConsumersAsync = false
            };
            var connection = connectionFactory.CreateConnection();
            _channel = connection.CreateModel();

        }

        public void Publish(string exchange, string routingKey, TRequest request)
        {
            string requestString = JsonConvert.SerializeObject(request);
            byte[] messageBytes = Encoding.UTF8.GetBytes(requestString);

            _channel.BasicPublish(
                exchange: exchange,
                routingKey: routingKey,
                basicProperties: null,
                body: messageBytes);
        }

        public string Call(string exchange, string routingKey, TRequest request)
        {
            var respQueue = new BlockingCollection<string>();
            string queueName = _channel.QueueDeclare().QueueName;
            _props = _channel.CreateBasicProperties();
            var correlationId = Guid.NewGuid().ToString();

            _props.CorrelationId = correlationId;
            _props.ReplyTo = queueName;

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += (model, ea) =>
            {
                if (ea.BasicProperties.CorrelationId == correlationId)
                {
                    respQueue.Add(Encoding.UTF8.GetString(ea.Body.ToArray()));
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
            };

            string requestString = JsonConvert.SerializeObject(request);
            byte[] messageBytes = Encoding.UTF8.GetBytes(requestString);

            _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
            _channel.BasicPublish(exchange: exchange, routingKey: routingKey, basicProperties: _props, body: messageBytes);

            return respQueue.Take();
        }
    }
}
