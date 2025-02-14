// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Google.Android.Material.TextField;
using System;
using System.Collections.Generic;

namespace AuthenticatorPro.Droid.Util
{
    internal static class TextInputUtil
    {
        public static void EnableAutoErrorClear(IEnumerable<TextInputLayout> layouts)
        {
            foreach (var layout in layouts)
            {
                EnableAutoErrorClear(layout);
            }
        }

        public static void EnableAutoErrorClear(TextInputLayout layout)
        {
            layout.EditText.TextChanged += delegate
            {
                if (!String.IsNullOrEmpty(layout.Error))
                {
                    layout.Error = null;
                }
            };
        }
    }
}