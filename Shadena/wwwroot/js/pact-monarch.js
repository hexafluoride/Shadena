window.pactLanguageSpec = {
  defaultToken: '',
  ignoreCase: true,
  tokenPostfix: '.scheme',

  brackets: [
    { open: '(', close: ')', token: 'delimiter.parenthesis' },
    { open: '{', close: '}', token: 'delimiter.curly' },
    { open: '[', close: ']', token: 'delimiter.square' }
  ],

  keywords: [
    'case',
    'do',
    'let',
    'loop',
    'if',
    'else',
    'when',
    'cons',
    'car',
    'cdr',
    'cond',
    'lambda',
    'lambda*',
    'syntax-rules',
    'format',
    'set!',
    'quote',
    'eval',
    'append',
    'list',
    'list?',
    'member?',
    'load',
    'namespace',
    'map',
    'try'
  ],

  types: [
    'integer', 'string', 'object', 'decimal'
  ],

  constants: ['#t', '#f'],

  operators: ['eq?', 'eqv?', 'equal?', 'and', 'or', 'not', 'null?'],

  tokenizer: {
    root: [
      [/#[xXoObB][0-9a-fA-F]+/, 'number.hex'],
      [/[+-]?\d+(?:(?:\.\d*)?(?:[eE][+-]?\d+)?)?/, 'number.float'],


{ include: '@whitespace' },
{ include: '@strings' },


[/(module)(\{)([0-9a-zA-Z\-]+)(\})/, ['type', 'type', 'type.identifier', 'type']],

// [
// 	/[a-zA-Z_#][a-zA-Z0-9_\-\?\!\*]*/,
// 	{
// 		cases: {
// 			'@keywords': { token: 'keyword', next: '@popAll' },
// 			'@constants': { token: 'constant', next: '@popAll' },
// 			'@operators': { token: 'operator', next: '@popAll' },
//       '@types': { token: 'type', next: '@popAll' }
// 		}
// 	}
// ],

[
/\(/,
{
token: 'white',
next: 'first'
}
],

[
/[a-zA-Z_#][a-zA-Z0-9_\-\?\!\*]*/,
{
cases: {
'@keywords': 'keyword',
'@constants': 'constant',
'@operators': 'operator',
'@types': 'type',
'@default': 'identifier'
}
}
],

],

first: [

[
/(?:\b(?:(module))\b)(\s+)((?:[a-zA-Z0-9\-]+))(\s+)((?:[a-zA-Z0-9\-]+))/,
['keyword', 'white', 'variable', 'white', 'identifier']
],
[
/(?:\b(?:(defun|defcap|defschema))\b)(\s+)((?:\w|\-|\!|\?)*)/,
['keyword', 'white', 'variable']
],
[/([a-zA-Z\-]+)(\:\:)/, ['variable', 'white'], '@popall'],
[
/[a-zA-Z_#][a-zA-Z0-9_\-\?\!\*]*/,
{
cases: {
'@keywords': 'keyword',
'@constants': 'constant',
'@operators': 'operator',
'@types': 'type',
'@default': 'keyword.directive'
}
}, '@popall'
],
[/([a-zA-Z\-]+)(\s+)/, ['tag', 'white'], '@popall'],
[/.?/, 'white', '@popall']
],

comment: [
[/[^\|#]+/, 'comment'],
[/#\|/, 'comment', '@push'],
[/\|#/, 'comment', '@pop'],
[/[\|#]/, 'comment']
],

whitespace: [
[/[ \t\r\n]+/, 'white'],
[/#\|/, 'comment', '@comment'],
[/;.*$/, 'comment']
],

strings: [
[/"$/, 'string', '@popall'],
[/'[a-zA-Z0-9\-]+/, 'string', '@popall'],
[/"(?=.)/, 'string', '@multiLineString']
],

multiLineString: [
[/[^\\"]+$/, 'string', '@popall'],
[/[^\\"]+/, 'string'],
[/\\./, 'string.escape'],
[/"/, 'string', '@popall'],
[/\\$/, 'string']
]
}
};