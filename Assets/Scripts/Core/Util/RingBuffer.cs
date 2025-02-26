using UnityEngine;

public class RingBuffer<T>
{
    private T[] m_buffer { get; }
    private int m_index { get; set; }

    public RingBuffer(T[] buffer)
    {
        m_buffer = buffer;
    }

    public T GetNext()
    {
        T t = m_buffer[m_index];
        m_index = (int)Mathf.Repeat(m_index + 1, m_buffer.Length);
        return t;
    }

    public T[] Values { get { return m_buffer; } }
}