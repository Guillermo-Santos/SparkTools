﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SparkCore.Analytics.Binding.Tree.Expressions;
using SparkCore.Analytics.Binding.Tree.Statements;

namespace SparkCore.Analytics.Binding.Tree;

internal abstract class BoundTreeRewriter
{
    public virtual BoundStatement RewriteStatement(BoundStatement node)
    {
        switch (node.Kind)
        {
            case BoundNodeKind.BlockStatement:
                return RewriteBlockStatement((BoundBlockStatement)node);
            case BoundNodeKind.NopStatement:
                return RewriteBoundNopStatement((BoundNopStatement)node);
            case BoundNodeKind.ExpressionStatement:
                return RewriteExpressionStatement((BoundExpressionStatement)node);
            case BoundNodeKind.LabelStatement:
                return RewriteLabelStatement((BoundLabelStatement)node);
            case BoundNodeKind.GotoStatement:
                return RewriteGotoStatement((BoundGotoStatement)node);
            case BoundNodeKind.ConditionalGotoStatement:
                return RewriteConditionalGotoStatement((BoundConditionalGotoStatement)node);
            case BoundNodeKind.ReturnStatement:
                return RewriteReturnStatement((BoundReturnStatement)node);
            case BoundNodeKind.VariableDeclaration:
                return RewriteVariableDeclaration((BoundVariableDeclaration)node);
            case BoundNodeKind.IfStatement:
                return RewriteIfStatement((BoundIfStatement)node);
            case BoundNodeKind.WhileStatement:
                return RewriteWhileStatement((BoundWhileStatement)node);
            case BoundNodeKind.DoWhileStatement:
                return RewriteDoWhileStatement((BoundDoWhileStatement)node);
            case BoundNodeKind.ForStatement:
                return RewriteForStatement((BoundForStatement)node);
            default:
                throw new Exception($"Unexpected node: {node.Kind}");
        }
    }


    protected virtual BoundStatement RewriteBlockStatement(BoundBlockStatement node)
    {
        List<BoundStatement>? builder = null;

        var statements = node.Statements.ToList();
        for (var i = 0; i < statements.Count; i++)
        {
            var oldStatement = statements[i];
            var newStatement = RewriteStatement(statements[i]);
            if (newStatement != oldStatement)
            {
                if (builder == null)
                {
                    builder = new(statements.Count);

                    for (var j = 0; j < i; j++)
                    {
                        builder.Add(statements[j]);
                    }
                }
            }

            if (builder != null)
                builder.Add(newStatement);
        }
        return builder == null ? (BoundStatement)node : new BoundBlockStatement(builder);
    }
    protected virtual BoundStatement RewriteBoundNopStatement(BoundNopStatement node)
    {
        return node;
    }
    protected virtual BoundStatement RewriteExpressionStatement(BoundExpressionStatement node)
    {
        var expression = RewriteExpression(node.Expression);
        if (expression == node.Expression)
            return node;
        return new BoundExpressionStatement(expression);

    }
    protected virtual BoundStatement RewriteLabelStatement(BoundLabelStatement node)
    {
        return node;
    }
    protected virtual BoundStatement RewriteGotoStatement(BoundGotoStatement node)
    {
        return node;
    }
    protected virtual BoundStatement RewriteConditionalGotoStatement(BoundConditionalGotoStatement node)
    {
        var condition = RewriteExpression(node.Condition);
        if (condition == node.Condition)
            return node;
        return new BoundConditionalGotoStatement(node.Label, condition, node.JumpIfTrue);
    }
    protected virtual BoundStatement RewriteReturnStatement(BoundReturnStatement node)
    {
        var expression = node.Expression == null ? null : RewriteExpression(node.Expression);
        if (expression == node.Expression)
            return node;
        return new BoundReturnStatement(expression);
    }
    protected virtual BoundStatement RewriteVariableDeclaration(BoundVariableDeclaration node)
    {
        var initializer = RewriteExpression(node.Initializer);
        if (initializer == node.Initializer)
            return node;
        return new BoundVariableDeclaration(node.Variable, initializer);
    }
    protected virtual BoundStatement RewriteIfStatement(BoundIfStatement node)
    {
        var condition = RewriteExpression(node.Condition);
        var thenStatement = RewriteStatement(node.ThenStatement);
        var elseStatement = node.ElseStatement == null ? null : RewriteStatement(node.ElseStatement);
        if (condition == node.Condition && thenStatement == node.ThenStatement && elseStatement == node.ElseStatement)
            return node;
        return new BoundIfStatement(condition, thenStatement, elseStatement);
    }
    protected virtual BoundStatement RewriteWhileStatement(BoundWhileStatement node)
    {
        var condition = RewriteExpression(node.Condition);
        var body = RewriteStatement(node.Body);
        if (condition == node.Condition && body == node.Body)
            return node;
        return new BoundWhileStatement(condition, body, node.BreakLabel, node.ContinueLabel);
    }
    protected virtual BoundStatement RewriteDoWhileStatement(BoundDoWhileStatement node)
    {
        var body = RewriteStatement(node.Body);
        var condition = RewriteExpression(node.Condition);

        if (body == node.Body && condition == node.Condition)
            return node;

        return new BoundDoWhileStatement(body, condition, node.BreakLabel, node.ContinueLabel);
    }
    protected virtual BoundStatement RewriteForStatement(BoundForStatement node)
    {
        var lowerBound = RewriteExpression(node.LowerBound);
        var upperBound = RewriteExpression(node.UpperBound);
        var body = RewriteStatement(node.Body);
        if (lowerBound == node.LowerBound && upperBound == node.UpperBound && body == node.Body)
            return node;
        return new BoundForStatement(node.Variable, lowerBound, upperBound, body, node.BreakLabel, node.ContinueLabel);
    }


    public virtual BoundExpression RewriteExpression(BoundExpression node)
    {
        switch (node.Kind)
        {
            case BoundNodeKind.ErrorExpression:
                return RewriteErrorExpression((BoundErrorExpression)node);
            case BoundNodeKind.LiteralExpression:
                return RewriteLiteralExpression((BoundLiteralExpression)node);
            case BoundNodeKind.UnaryExpression:
                return RewriteUnarylExpression((BoundUnaryExpression)node);
            case BoundNodeKind.BinaryExpression:
                return RewriteBinaryExpression((BoundBinaryExpression)node);
            case BoundNodeKind.VariableExpression:
                return RewriteVariableExpression((BoundVariableExpression)node);
            case BoundNodeKind.AssignmentExpression:
                return RewriteAssignmentExpression((BoundAssignmentExpression)node);
            case BoundNodeKind.CallExpression:
                return RewriteCallExpression((BoundCallExpression)node);
            case BoundNodeKind.ConversionExpression:
                return RewriteConversionExpression((BoundConversionExpression)node);
            default:
                throw new Exception($"Unexpected node: {node.Kind}");
        }
    }
    protected virtual BoundExpression RewriteErrorExpression(BoundErrorExpression node)
    {
        return node;
    }
    protected virtual BoundExpression RewriteLiteralExpression(BoundLiteralExpression node)
    {
        return node;
    }
    protected virtual BoundExpression RewriteVariableExpression(BoundVariableExpression node)
    {
        return node;
    }
    protected virtual BoundExpression RewriteUnarylExpression(BoundUnaryExpression node)
    {
        var operand = RewriteExpression(node.Operand);
        if (operand == node.Operand)
            return node;
        return new BoundUnaryExpression(node.Op, operand);
    }
    protected virtual BoundExpression RewriteBinaryExpression(BoundBinaryExpression node)
    {
        var left = RewriteExpression(node.Left);
        var right = RewriteExpression(node.Right);
        if (left == node.Left && right == node.Right)
            return node;
        return new BoundBinaryExpression(left, node.Op, right);
    }
    protected virtual BoundExpression RewriteAssignmentExpression(BoundAssignmentExpression node)
    {
        var expression = RewriteExpression(node.Expression);
        if (expression == node.Expression)
            return node;
        return new BoundAssignmentExpression(node.Variable, expression);
    }
    protected virtual BoundExpression RewriteCallExpression(BoundCallExpression node)
    {
        List<BoundExpression>? builder = null;

        var arguments = node.Arguments.ToList();
        for (var i = 0; i < arguments.Count; i++)
        {
            var oldArgument = arguments[i];
            var newArgument = RewriteExpression(arguments[i]);
            if (newArgument != oldArgument)
            {
                if (builder == null)
                {
                    builder = new List<BoundExpression>(arguments.Count);

                    for (var j = 0; j < i; j++)
                        builder.Add(arguments[j]);
                }
            }

            if (builder != null)
                builder.Add(newArgument);
        }
        return builder == null ? (BoundExpression)node : new BoundCallExpression(node.Function, builder);
    }
    protected virtual BoundExpression RewriteConversionExpression(BoundConversionExpression node)
    {
        var expression = RewriteExpression(node.Expression);
        if (expression == node.Expression)
            return node;
        return new BoundConversionExpression(node.Type, expression);
    }

}