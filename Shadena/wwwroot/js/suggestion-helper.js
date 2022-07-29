window.attachInstance = function(dotnetHelper) {
  window.completionAttached = true;
  window.fetchSymbolsAtLocation = function (location) {
    return dotnetHelper.invokeMethodAsync('GetSymbolsAtLocation', location.lineNumber, location.column);  
  };
};

// this stuff is especially incomplete
window.getSignature = function (name, words) {
  console.log('name ' + name + ' words: ' + words.join(','));
};

window.signatureHelper = {
  signatureHelpTriggerCharacters: [' '],
  // private static _toSignatureHelpTriggerReason(
  //     context: languages.SignatureHelpContext
  // ): ts.SignatureHelpTriggerReason {
  //   switch (context.triggerKind) {
  //     case languages.SignatureHelpTriggerKind.TriggerCharacter:
  //       if (context.triggerCharacter) {
  //         if (context.isRetrigger) {
  //           return { kind: 'retrigger', triggerCharacter: context.triggerCharacter as any };
  //         } else {
  //           return { kind: 'characterTyped', triggerCharacter: context.triggerCharacter as any };
  //         }
  //       } else {
  //         return { kind: 'invoked' };
  //       }
  //
  //     case languages.SignatureHelpTriggerKind.ContentChange:
  //       return context.isRetrigger ? { kind: 'retrigger' } : { kind: 'invoked' };
  //
  //     case languages.SignatureHelpTriggerKind.Invoke:
  //     default:
  //       return { kind: 'invoked' };
  //   }
  // }
  
  trimString: function(str) {
    return str.trim().split('').reverse().join('');
  },

  provideSignatureHelp: async function(
      model,
      position,
      token,
      context
  ) {
    const offset = model.getOffsetAt(position);

    if (model.isDisposed()) {
      return;
    }
    
    const grabStart = model.getPositionAt(offset - 100);
    const range = {
      startLineNumber: grabStart.lineNumber,
      endLineNumber: position.lineNumber,
      startColumn: grabStart.column,
      endColumn: position.column
    };
    const rangeText = model.getValueInRange(range);
    
    let wordsReversed = [];
    let currentWord = '';
    
    let balance = 1;
    
    for (let i = rangeText.length - 1; i >= 0; i--) {
      if (balance === 0)
        break;
      
      if (rangeText[i] === '(' || rangeText[i] === ')' || rangeText[i] === ' ' || rangeText[i] === '\n' || rangeText[i] === '\t' || rangeText[i] === '\r') {

        if (balance === 1)
        {
          var wordClear = this.trimString(currentWord);
          if (wordClear !== '') {
            currentWord = '';
            wordsReversed.push(wordClear);
          }
        }

        if (rangeText[i] === ')') {
          balance++;
        }

        if (rangeText[i] === '(') {
          balance--;
        }
      }
      
      currentWord += rangeText[i];
    }
    
    // currentWord = this.trimString(currentWord);
    // if (currentWord !== '')
    //   wordsReversed.push(currentWord);
    
    const info = window.getSignature(wordsReversed[wordsReversed.length - 1], wordsReversed);

    if (wordsReversed.length === 0)
      return;

    if (!info || model.isDisposed()) {
      return;
    }

    const ret = {
      activeSignature: info.selectedItemIndex,
      activeParameter: info.argumentIndex,
      signatures: []
    };

    info.items.forEach((item) => {
      const signature = {
        label: '',
        parameters: []
      };

      item.parameters.forEach((p, i, a) => {
        const label = p.name;
        const parameter = {
          label: label,
          documentation: {
            value: ''
          }
        };
        signature.label += label;
        signature.parameters.push(parameter);
        if (i < a.length - 1) {
          signature.label += ' ';
        }
      });
      ret.signatures.push(signature);
    });

    return {
      value: ret,
      dispose() {}
    };
  }
}

//
// function createDependencyProposals(range) {
//     // returning a static list of proposals, not even looking at the prefix (filtering is done by the Monaco editor),
//     // here you could do a server side lookup
//     return [
//         {
//             label: '"lodash"',
//             kind: monaco.languages.CompletionItemKind.Function,
//             documentation: 'The Lodash library exported as Node.js modules.',
//             insertText: '"lodash": "*"',
//             range: range
//         },
//         {
//             label: '"express"',
//             kind: monaco.languages.CompletionItemKind.Function,
//             documentation: 'Fast, unopinionated, minimalist web framework',
//             insertText: '"express": "*"',
//             range: range
//         },
//         {
//             label: '"mkdirp"',
//             kind: monaco.languages.CompletionItemKind.Function,
//             documentation: 'Recursively mkdir, like <code>mkdir -p</code>',
//             insertText: '"mkdirp": "*"',
//             range: range
//         },
//         {
//             label: '"my-third-party-library"',
//             kind: monaco.languages.CompletionItemKind.Function,
//             documentation: 'Describe your library here',
//             insertText: '"${1:my-third-party-library}": "${2:1.2.3}"',
//             insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
//             range: range
//         }
//     ];
// }
//

// monaco.languages.registerCompletionItemProvider('pact', {
//     provideCompletionItems: function (model, position) {
//         // find out if we are completing a property in the 'dependencies' object.
//         var textUntilPosition = model.getValueInRange({
//             startLineNumber: 1,
//             startColumn: 1,
//             endLineNumber: position.lineNumber,
//             endColumn: position.column
//         });
//         var match = textUntilPosition.match(
//             /"dependencies"\s*:\s*\{\s*("[^"]*"\s*:\s*"[^"]*"\s*,\s*)*([^"]*)?$/
//         );
//         if (!match) {
//             return { suggestions: [] };
//         }
//         var word = model.getWordUntilPosition(position);
//         var range = {
//             startLineNumber: position.lineNumber,
//             endLineNumber: position.lineNumber,
//             startColumn: word.startColumn,
//             endColumn: word.endColumn
//         };
//         return {
//             suggestions: createDependencyProposals(range)
//         };
//     }
// });
//
// monaco.editor.create(document.getElementById('container'), {
//     value: '{\n\t"dependencies": {\n\t\t\n\t}\n}\n',
//     language: 'json'
// });
