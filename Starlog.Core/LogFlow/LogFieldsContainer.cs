using System.Collections.Immutable;

using FieldNameHash = int;

namespace Genius.Starlog.Core.LogFlow;

public interface ILogFieldsContainerReadonly
{
    /// <summary>
    ///   Returns the current count of fields.
    /// </summary>
    int GetFieldCount();

    /// <summary>
    ///   Returns a field name per specified <paramref name="fieldId" />.
    /// </summary>
    /// <param name="fieldId">The field id (a zero-based index).</param>
    string GetFieldName(FieldId fieldId);

    /// <summary>
    ///   Returns fields with their id's and names.
    /// </summary>
    ImmutableArray<(FieldId FieldId, string FieldName)> GetFields();

    /// <summary>
    ///   Returns a field value.
    /// </summary>
    /// <param name="fieldId">The field id (a zero-based index).</param>
    /// <param name="fieldValueId">The field value id (a zero-based index).</param>
    string GetFieldValue(FieldId fieldId, FieldValueId fieldValueId);

    /// <summary>
    ///   Returns all field values per specified <paramref name="fieldId" />.
    /// </summary>
    /// <param name="fieldId">The field id (a zero-based index).</param>
    ImmutableArray<string> GetFieldValues(FieldId fieldId);

    /// <summary>
    ///   Returns a field identifier with its name of the "THREAD" field, if it is available in the profile's context.
    /// </summary>
    (FieldId FieldId, string FieldName)? GetThreadFieldIfAny();
}

internal interface ILogFieldsContainer : ILogFieldsContainerReadonly
{
    /// <summary>
    ///   Adds a field value if it was not added before, otherwise just returns previously assigned field value id.
    /// </summary>
    /// <param name="fieldId">The field id (a zero-based index).</param>
    /// <param name="value">The value of the field.</param>
    /// <returns>An index of the field value.</returns>
    FieldValueId AddFieldValue(FieldId fieldId, string value);
    void Clear();
    FieldId GetOrAddFieldId(string fieldName);
    void RemoveFieldValue(FieldId fieldId, FieldValueId fieldValueId);
}

internal sealed class LogFieldsContainer : ILogFieldsContainer
{
    private readonly object fieldAccessingLock = new();
    private readonly Dictionary<FieldNameHash, FieldId> _fieldIdPerFieldNameHash = [];
    private List<string> _fieldMapping = [];
    private List<List<string>> _fieldValues = [];
    private List<Dictionary<string, FieldValueId>> _fieldValuesToIndicesMapping = [];

    public FieldValueId AddFieldValue(FieldId fieldId, string value)
    {
        lock (fieldAccessingLock)
        {
            var fieldValues = _fieldValues[fieldId];
            var fieldValueId = fieldValues.IndexOf(value);
            if (fieldValueId == -1)
            {
                _fieldValues[fieldId].Add(value);
                fieldValueId = _fieldValues[fieldId].Count - 1;
                _fieldValuesToIndicesMapping[fieldId].Add(value, fieldValueId);
            }

            return fieldValueId;
        }
    }

    public void Clear()
    {
        _fieldIdPerFieldNameHash.Clear();
        _fieldMapping.Clear();
        _fieldValues.Clear();
        _fieldValuesToIndicesMapping.Clear();
    }

    public int GetFieldCount()
        => _fieldMapping.Count;

    public string GetFieldName(FieldId fieldId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_fieldMapping.Count, fieldId);
        return _fieldMapping[fieldId];
    }

    public ImmutableArray<(FieldId FieldId, string FieldName)> GetFields()
    {
        return _fieldMapping.Select((fieldName, fieldId) => (fieldId, fieldName)).ToImmutableArray();
    }

    public string GetFieldValue(FieldId fieldId, FieldValueId fieldValueId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_fieldValues.Count, fieldId);
        var fieldValues = _fieldValues[fieldId];

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(fieldValues.Count, fieldValueId);
        return fieldValues[fieldValueId];
    }

    public ImmutableArray<string> GetFieldValues(FieldId fieldId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_fieldValues.Count, fieldId);
        return _fieldValues[fieldId].ToImmutableArray();
    }

    public FieldId GetOrAddFieldId(string fieldName)
    {
        lock (fieldAccessingLock)
        {
            var hashCode = fieldName.GetHashCode();
            if (_fieldIdPerFieldNameHash.TryGetValue(hashCode, out var fieldId))
            {
                return fieldId;
            }

            _fieldMapping.Add(fieldName);
            _fieldValues.Add([]);
            _fieldValuesToIndicesMapping.Add([]);
            fieldId = _fieldMapping.Count - 1;
            _fieldIdPerFieldNameHash.Add(hashCode, fieldId);
            return fieldId;
        }
    }

    public (FieldId FieldId, string FieldName)? GetThreadFieldIfAny()
    {
        for (var fieldId = 0; fieldId < _fieldMapping.Count; fieldId++)
        {
            var fieldName = _fieldMapping[fieldId];
            if (fieldName.StartsWith("thread", StringComparison.OrdinalIgnoreCase))
                return (FieldId: fieldId, FieldName: fieldName);
        }

        return null;
    }

    public void RemoveFieldValue(FieldId fieldId, int fieldValueId)
    {
        var fieldValue = _fieldValues[fieldId][fieldValueId];
        _fieldValuesToIndicesMapping[fieldId].Remove(fieldValue);

        // We don't do `_fieldValues[fieldId].RemoveAt(fieldValueId)` otherwise it will desync
        // with _fieldValuesToIndicesMapping. For now we marking dead values with null.
        _fieldValues[fieldId][fieldValueId] = default!;
    }

    public void UpdateFrom(ILogFieldsContainer fields)
    {
        var fieldsContainer = (LogFieldsContainer)fields;
        _fieldMapping = fieldsContainer._fieldMapping;
        _fieldValues = fieldsContainer._fieldValues;
        _fieldValuesToIndicesMapping = fieldsContainer._fieldValuesToIndicesMapping;
    }
}
