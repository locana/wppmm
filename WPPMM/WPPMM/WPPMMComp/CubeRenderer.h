#pragma once

#include "Direct3DBase.h"
#include "SpriteBatch.h"
#include "SpriteFont.h"

struct ModelViewProjectionConstantBuffer
{
	DirectX::XMFLOAT4X4 model;
	DirectX::XMFLOAT4X4 view;
	DirectX::XMFLOAT4X4 projection;
};

struct VertexPositionColor
{
	DirectX::XMFLOAT3 pos;
	DirectX::XMFLOAT3 color;
};

struct Vertex	//Overloaded Vertex Structure
{
	Vertex(){}
	Vertex(float x, float y, float z,
		float u, float v)
		: pos(x,y,z), texCoord(u, v){}

	DirectX::XMFLOAT3 pos;
	DirectX::XMFLOAT2 texCoord;
};


// このクラスは、スピンしている立方体を描画します。
ref class CubeRenderer sealed : public Direct3DBase
{

public:
	CubeRenderer();

	// Direct3DBase メソッド。
	virtual void CreateDeviceResources() override;
	virtual void CreateWindowSizeDependentResources() override;
	virtual void Render() override;
	
	// 時間に依存するオブジェクトを更新するメソッドです。
	void Update(float timeTotal, float timeDelta);


	void setScreenData(const Platform::Array<byte>^ data);
	void setTexture(int* buffer, int width, int height);

	int GetDebugValue();

private:

	int ConvertErrorCode (HRESULT hr);

	bool m_loadingComplete;
	long hResult;


	Microsoft::WRL::ComPtr<ID3D11ShaderResourceView> m_texture1;
	std::unique_ptr<DirectX::SpriteBatch> spriteBatch;
	std::unique_ptr<DirectX::SpriteFont> m_font;

	D3D11_SUBRESOURCE_DATA screenResource;
	Microsoft::WRL::ComPtr<ID3D11Texture2D> m_texture2d;
	
	static const int SCREEN_BUF_MAX = 1000000; // 1MB
	// Platform::Array<byte>^ screen_buf;
	uint8_t screen_buf[SCREEN_BUF_MAX];
	int screen_buf_size;
	int debugValue;
	int debugCount;
	bool flag;

	Microsoft::WRL::ComPtr<ID3D11InputLayout> m_inputLayout;
	Microsoft::WRL::ComPtr<ID3D11Buffer> m_vertexBuffer;
	Microsoft::WRL::ComPtr<ID3D11Buffer> m_indexBuffer;
	Microsoft::WRL::ComPtr<ID3D11VertexShader> m_vertexShader;
	Microsoft::WRL::ComPtr<ID3D11PixelShader> m_pixelShader;
	Microsoft::WRL::ComPtr<ID3D11Buffer> m_constantBuffer;

	uint32 m_indexCount;
	ModelViewProjectionConstantBuffer m_constantBufferData;
	Microsoft::WRL::ComPtr<ID3D11ShaderResourceView> SRV;
	Microsoft::WRL::ComPtr<ID3D11SamplerState> CubesTexSamplerState;

	Microsoft::WRL::ComPtr<ID3D11BlendState> Transparency;
	Microsoft::WRL::ComPtr<ID3D11RasterizerState> CCWcullMode;
	Microsoft::WRL::ComPtr<ID3D11RasterizerState> CWcullMode;
};
