﻿using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Collections.Generic;
using GitHub.InlineReviews.Glyph;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Classification;
using GitHub.InlineReviews.Services;
using GitHub.Models;

namespace GitHub.InlineReviews.Tags
{
    class InlineCommentGlyphFactory : IGlyphFactory<InlineCommentTag>
    {
        readonly IInlineCommentPeekService peekService;
        readonly ITextView textView;
        readonly BrushesManager brushesManager;

        public InlineCommentGlyphFactory(
            IInlineCommentPeekService peekService,
            ITextView textView,
            IEditorFormatMap editorFormatMap)
        {
            this.peekService = peekService;
            this.textView = textView;

            brushesManager = new BrushesManager(editorFormatMap);
        }

        class BrushesManager
        {
            readonly Brush addBackground;
            readonly Brush deleteBackground;
            readonly Brush noneBackground;

            internal BrushesManager(IEditorFormatMap editorFormatMap)
            {
                addBackground = TryGetValue<Brush>(editorFormatMap, "deltadiff.add.word", "Background");
                deleteBackground = TryGetValue<Brush>(editorFormatMap, "deltadiff.remove.word", "Background");
                noneBackground = TryGetValue<Brush>(editorFormatMap, "Indicator Margin", "Background");
            }

            T TryGetValue<T>(IEditorFormatMap editorFormatMap, string key, string name) where T : class
            {
                var properties = editorFormatMap.GetProperties(key);
                return properties?[name] as T;
            }

            internal Brush GetBrush(DiffChangeType diffChangeType)
            {
                switch (diffChangeType)
                {
                    case DiffChangeType.Add:
                        return addBackground;
                    case DiffChangeType.Delete:
                        return deleteBackground;
                    case DiffChangeType.None:
                    default:
                        return noneBackground;
                }
            }
        }

        public UIElement GenerateGlyph(IWpfTextViewLine line, InlineCommentTag tag)
        {
            var glyph = CreateGlyph(tag);
            glyph.Tag = tag;

            glyph.MouseLeftButtonUp += (s, e) =>
            {
                if (OpenThreadView(tag)) e.Handled = true;
            };

            glyph.Background = brushesManager.GetBrush(tag.DiffChangeType);
            return glyph;
        }

        public IEnumerable<Type> GetTagTypes()
        {
            return new[]
            {
                typeof(AddInlineCommentTag),
                typeof(ShowInlineCommentTag)
            };
        }

        static UserControl CreateGlyph(InlineCommentTag tag)
        {
            var addTag = tag as AddInlineCommentTag;
            var showTag = tag as ShowInlineCommentTag;

            if (addTag != null)
            {
                return new AddInlineCommentGlyph();
            }
            else if (showTag != null)
            {
                return new ShowInlineCommentGlyph()
                {
                    Opacity = showTag.Thread.IsStale ? 0.5 : 1,
                };
            }

            throw new ArgumentException($"Unknown 'InlineCommentTag' type '{tag}'");
        }

        bool OpenThreadView(InlineCommentTag tag)
        {
            var addTag = tag as AddInlineCommentTag;
            var showTag = tag as ShowInlineCommentTag;

            if (addTag != null)
            {
                peekService.Show(textView, addTag);
                return true;
            }
            else if (showTag != null)
            {
                peekService.Show(textView, showTag);
                return true;
            }

            return false;
        }
    }
}
