﻿using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;
using static FastEndpoints.Constants;

namespace FastEndpoints;

public abstract partial class Endpoint<TRequest, TResponse> : BaseEndpoint where TRequest : notnull, new() where TResponse : notnull
{
    private static async Task RunPostProcessors(List<object> postProcessors, TRequest req, TResponse resp, HttpContext ctx, List<ValidationFailure> validationFailures, CancellationToken cancellation)
    {
        for (var i = 0; i < postProcessors.Count; i++)
        {
            switch (postProcessors[i])
            {
                case IGlobalPostProcessor gp:
                    await gp.PostProcessAsync(req, resp, ctx, validationFailures, cancellation);
                    break;
                case IPostProcessor<TRequest, TResponse> pp:
                    await pp.PostProcessAsync(req, resp, ctx, validationFailures, cancellation);
                    break;
            }
        }
    }

    private static async Task RunPreprocessors(List<object> preProcessors, TRequest req, HttpContext ctx, List<ValidationFailure> validationFailures, CancellationToken cancellation)
    {
        for (var i = 0; i < preProcessors.Count; i++)
        {
            switch (preProcessors[i])
            {
                case IGlobalPreProcessor gp:
                    await gp.PreProcessAsync(req, ctx, validationFailures, cancellation);
                    break;
                case IPreProcessor<TRequest> pr:
                    await pr.PreProcessAsync(req, ctx, validationFailures, cancellation);
                    break;
            }
        }
    }

    private static Task AutoSendResponse(HttpContext ctx, TResponse? responseDto, JsonSerializerContext? jsonSerializerContext, CancellationToken cancellation)
    {
        return responseDto is null
               ? ctx.Response.SendNoContentAsync(cancellation)
               : ctx.Response.SendAsync(responseDto, 200, jsonSerializerContext, cancellation);
    }

    private static readonly Action<RouteHandlerBuilder> ClearDefaultAcceptsProducesMetadata = b =>
    {
        b.Add(epBuilder =>
        {
            foreach (var m in epBuilder.Metadata.Where(o => o.GetType().Name is ProducesMetadata or AcceptsMetaData).ToArray())
                epBuilder.Metadata.Remove(m);
        });
    };
}