﻿using Nest;
using Nethereum.BlockchainProcessing.BlockchainProxy;
using Nethereum.BlockchainProcessing.Processing;
using Nethereum.RPC.Eth.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nethereum.BlockchainProcessing.Handlers;
using Nethereum.Contracts;

namespace Nethereum.BlockchainStore.Search.ElasticSearch
{
    public class ElasticEventIndexingProcessor : EventIndexingProcessor
    {
        private readonly IElasticSearchService _elasticSearchService;

        public ElasticEventIndexingProcessor(
            IElasticClient elasticClient, 
            string blockchainUrl, 
            uint maxBlocksPerBatch = 2, 
            IEnumerable<NewFilterInput> filters = null,
            uint minBlockConfirmations = 0)
            :this(new BlockchainProxyService(blockchainUrl), 
                new ElasticSearchService(elasticClient), 
                null,
                null, 
                maxBlocksPerBatch, 
                filters,
                minBlockConfirmations)
        {
        }

        public ElasticEventIndexingProcessor(
            IBlockchainProxyService blockchainProxyService, 
            IElasticSearchService searchService, 
            IEventFunctionProcessor functionProcessor, 
            Func<ulong, ulong?, IBlockProgressService> blockProgressServiceCallBack = null, 
            uint maxBlocksPerBatch = 2, 
            IEnumerable<NewFilterInput> filters = null, 
            uint minimumBlockConfirmations = 0) : 
            base(blockchainProxyService, searchService, functionProcessor, blockProgressServiceCallBack, maxBlocksPerBatch, filters, minimumBlockConfirmations)
        {
            this._elasticSearchService = searchService;
        }

        public async Task<IEventIndexProcessor<TEvent>> AddAsync<TEvent, TSearchDocument>(
            string indexName, Func<EventLog<TEvent>, TSearchDocument> mappingFunc,
            IEnumerable<ITransactionHandler> functionHandlers = null) where TEvent : class, new() 
            where TSearchDocument : class, IHasId, new()
        {

            var indexer = await _elasticSearchService.CreateEventIndexer(indexName, mappingFunc);
            _indexers.Add(indexer);
            
            return CreateProcessor(functionHandlers, indexer);
        }

        public async Task<IEventIndexProcessor<TEvent>> AddAsync<TEvent, TSearchDocument>(
            string indexName, IEventToSearchDocumentMapper<TEvent, TSearchDocument> mapper,
            IEnumerable<ITransactionHandler> functionHandlers = null) 
            where TEvent : class, new() where TSearchDocument : class, IHasId, new()
        {

            var indexer = await _elasticSearchService.CreateEventIndexer(indexName, mapper);
            _indexers.Add(indexer);
            
            return CreateProcessor(functionHandlers, indexer);
        }

        public async Task<FunctionIndexTransactionHandler<TFunctionMessage>> CreateFunctionHandlerAsync<TFunctionMessage, TSearchDocument>(
            string indexName, Func<FunctionCall<TFunctionMessage>, TSearchDocument> mappingFunc)
            where TFunctionMessage : FunctionMessage, new()
            where TSearchDocument : class, IHasId, new()
        {
            var functionIndexer = await _elasticSearchService.CreateFunctionIndexer<TFunctionMessage, TSearchDocument>(indexName, mappingFunc);
            var functionHandler = new FunctionIndexTransactionHandler<TFunctionMessage>(functionIndexer);
            return functionHandler;
        }

        public async Task<FunctionIndexTransactionHandler<TFunctionMessage>> CreateFunctionHandlerAsync<TFunctionMessage, TSearchDocument>(
            string indexName, IFunctionMessageToSearchDocumentMapper<TFunctionMessage, TSearchDocument> mapper)
            where TFunctionMessage : FunctionMessage, new()
            where TSearchDocument : class, IHasId, new()
        {
            var functionIndexer = await _elasticSearchService.CreateFunctionIndexer<TFunctionMessage, TSearchDocument>(indexName, mapper);
            var functionHandler = new FunctionIndexTransactionHandler<TFunctionMessage>(functionIndexer);
            return functionHandler;
        }
    }
}
