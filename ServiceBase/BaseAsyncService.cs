using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Types;
using JsonException = System.Text.Json.JsonException;

namespace BaseAsyncServices.ServiceBase
{
    public abstract class BaseAsyncService<TRequest> : BaseAsyncBackgroundService
    {
        protected BaseAsyncService(ILogger<BaseAsyncService<TRequest>> logger) : base(logger)
        {
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new AsyncEventingBasicConsumer(Channel);
            consumer.Received += async (bc, ea) =>
            {
                string message = Encoding.UTF8.GetString(ea.Body.ToArray());
                try
                {
                    var request = JsonConvert.DeserializeObject<TRequest>(message);
                    await ConsumerHandleAsync(request);
                    Channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (JsonException)
                {
                    Logger.LogError($"JSON parse error1 object to type TRequest {message}");
                    Channel.BasicNack(ea.DeliveryTag, false, false);
                }
                catch (AlreadyClosedException)
                {
                    Logger.LogInformation("RabbitMQ is closed!");
                }
                catch (Exception e)
                {
                    Logger.LogError(default, e, e.Message);
                }
            };
            Channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);
            await Task.CompletedTask;
        }

        protected abstract Task ConsumerHandleAsync(TRequest request);
    }

    public abstract class BaseAsyncService<TRequest, TResponce> : BaseAsyncBackgroundService
    {
        protected BaseAsyncService(ILogger<BaseAsyncService<TRequest, TResponce>> logger) : base(logger)
        {
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new AsyncEventingBasicConsumer(Channel);
            consumer.Received += async (bc, ea) =>
            {
                string request = Encoding.UTF8.GetString(ea.Body.ToArray());

                IBasicProperties props = ea.BasicProperties;
                IBasicProperties replyProps = Channel.CreateBasicProperties();

                replyProps.CorrelationId = props.CorrelationId;
                TRequest message;
                try
                {
                    message = JsonConvert.DeserializeObject<TRequest>(request);
                    TResponse response = await ConsumerHandleAsync(message);

                    string responseString = JsonConvert.SerializeObject(response);
                    byte[] responseBytes = Encoding.UTF8.GetBytes(responseString);

                    Channel.BasicAck(ea.DeliveryTag, false);
                    if (ea.BasicProperties.ReplyTo != null)
                    {
                        Channel.BasicPublish(exchange: "", routingKey: ea.BasicProperties.ReplyTo,
                            basicProperties: ea.BasicProperties, body: responseBytes);
                    }
                    else
                    {
                        Logger.LogWarning("Brak zdefiniowanej kolejki zwrotnej");
                    }
                }
                catch (JsonException)
                {
                    Logger.LogError($"JSON parse error 2 object to type TRequest {request}.");
                    Channel.BasicNack(ea.DeliveryTag, false, false);
                }
                catch (AlreadyClosedException)
                {
                    Logger.LogInformation("RabbitMQ is closed!");
                }
                catch (Exception e)
                {
                    Logger.LogError(default, e, e.Message);
                }
            };
            Channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);
            await Task.CompletedTask;
        }

        protected abstract Task<TResponse> ConsumerHandleAsync(TRequest request);
    }
}
