#pragma once

#include <wrl.h>

// 基本タイミング用のヘルパー クラス。
ref class BasicTimer sealed
{
public:
	// 内部タイマー値を初期化します。
	BasicTimer()
	{
		if (!QueryPerformanceFrequency(&m_frequency))
		{
			throw ref new Platform::FailureException();
		}
		Reset();
	}
	
	// タイマーを初期値にリセットします。
	void Reset()
	{
		Update();
		m_startTime = m_currentTime;
		m_total = 0.0f;
		m_delta = 1.0f / 60.0f;
	}
	
	// タイマーの内部値を更新します。
	void Update()
	{
		if (!QueryPerformanceCounter(&m_currentTime))
		{
			throw ref new Platform::FailureException();
		}
		
		m_total = static_cast<float>(
			static_cast<double>(m_currentTime.QuadPart - m_startTime.QuadPart) /
			static_cast<double>(m_frequency.QuadPart)
			);
		
		if (m_lastTime.QuadPart == m_startTime.QuadPart)
		{
			// タイマーがリセットされた場合は、60 Hz のフレーム時間に相当する時間差を報告します。
			m_delta = 1.0f / 60.0f;
		}
		else
		{
			m_delta = static_cast<float>(
				static_cast<double>(m_currentTime.QuadPart - m_lastTime.QuadPart) /
				static_cast<double>(m_frequency.QuadPart)
				);
		}
		
		m_lastTime = m_currentTime;
	}
	
	// Reset() への最後の呼び出しと Update() への最後の呼び出しの間の間隔 (秒単位)。
	property float Total
	{
		float get() { return m_total; }
	}
	
	// Update() に対する直前の 2 回の呼び出しの間の間隔 (秒単位)。
	property float Delta
	{
		float get() { return m_delta; }
	}

private:
	LARGE_INTEGER m_frequency;
	LARGE_INTEGER m_currentTime;
	LARGE_INTEGER m_startTime;
	LARGE_INTEGER m_lastTime;
	float m_total;
	float m_delta;
};
