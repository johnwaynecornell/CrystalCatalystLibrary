// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
public class StringCursor
{
    private readonly string _source;
    private int _position;

    public StringCursor(string source)
    {
        _source = source;
        _position = 0;
    }

    public bool IsEnd => _position >= _source.Length;

    public char Current => _source[_position];

    public char Take()
    {
        return _source[_position++];
    }

    public void SkipWhitespace()
    {
        while (_position < _source.Length && char.IsWhiteSpace(_source[_position]))
        {
            _position++;
        }
    }

    public void Advance(int count)
    {
        _position += count;
    }
}