﻿using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Contour.Testing.Transport.RabbitMq;

using FluentAssertions;

using NUnit.Framework;

namespace Contour.RabbitMq.Tests
{
    /// <summary>
    /// The performance publishing specs.
    /// </summary>
    // ReSharper disable InconsistentNaming
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Reviewed. Suppression is OK here.")]
    public class PerformancePublishingSpecs
    {
        /// <summary>
        /// The given_consumer_and_producer_are_created.
        /// </summary>
        public class given_consumer_and_producer_are_created : RabbitMqFixture
        {
            #region Fields

            /// <summary>
            /// The consumer.
            /// </summary>
            public IBus Consumer;

            /// <summary>
            /// The producer.
            /// </summary>
            public IBus Producer;

            #endregion

            #region Constructors and Destructors

            /// <summary>
            /// Инициализирует новый экземпляр класса <see cref="given_consumer_and_producer_are_created"/>.
            /// </summary>
            public given_consumer_and_producer_are_created()
            {
                this.Producer = this.StartBus(
                    "producer",
                    cfg =>
                        {
                            cfg.UseParallelismLevel(4);

                            cfg.Route("dummy.request").
                                WithConfirmation();
                        });

                this.Consumer = this.StartBus(
                    "consumer",
                    cfg =>
                        {
                            cfg.UseParallelismLevel(4);

                            cfg.On<DummyRequest>("dummy.request").
                                ReactWith((m, ctx) => ctx.Accept()).
                                RequiresAccept();
                        });
            }

            #endregion
        }

        /// <summary>
        /// The when_publishing_many_messages_reliable.
        /// </summary>
        [TestFixture]
        [Category("Integration")]
        public class when_publishing_many_messages_reliable : given_consumer_and_producer_are_created
        {
            #region Public Methods and Operators

            /// <summary>
            /// The should_process_fast_enough.
            /// </summary>
            [Test]
            [Ignore("Too long")]
            public void should_process_fast_enough()
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                int total = 0;
                const int totalOps = 30000;

                Task[] tasks = Enumerable.Range(0, totalOps).
                    Select(
                        n => this.Producer.Emit("dummy.request", new DummyRequest(n)).
                                 ContinueWith(t => { Interlocked.Increment(ref total); })).
                    ToArray();

                Task.WaitAll(tasks);
                stopwatch.Stop();

                total.Should().
                    Be(totalOps);

                double opsPerSecond = totalOps / stopwatch.Elapsed.TotalSeconds;

                Console.WriteLine("{0} ops/s", opsPerSecond);

                opsPerSecond.Should().
                    BeGreaterThan(6000);
            }

            #endregion
        }
    }

    // ReSharper restore InconsistentNaming
}
