#include "pch.h"
#include "CubeRenderer.h"
#include "DDSTextureLoader.h"

using namespace DirectX;
using namespace Microsoft::WRL;
using namespace Windows::Foundation;
using namespace Windows::UI::Core;

CubeRenderer::CubeRenderer()
{
	
}

void CubeRenderer::CreateDeviceResources()
{
	Direct3DBase::CreateDeviceResources();

	    // Create DirectXTK objects
    auto device = m_d3dDevice.Get();
	auto context = m_d3dContext.Get();

	m_font.reset( new SpriteFont( device, L"Assets\\italic.spritefont" ) );
	
	spriteBatch.reset( new SpriteBatch( context ) );
		
	DX::ThrowIfFailed(
        CreateDDSTextureFromFile( device, L"Assets\\windowslogo.dds", nullptr, m_texture1.ReleaseAndGetAddressOf() )
		);

	
	
	screen_buf_size = 0;
	hResult = 0;
}

void CubeRenderer::CreateWindowSizeDependentResources()
{
	Direct3DBase::CreateWindowSizeDependentResources();


}

void CubeRenderer::Update(float timeTotal, float timeDelta)
{

}

void CubeRenderer::Render()
{    // clear
	m_d3dContext->OMSetRenderTargets(
		1,
		m_renderTargetView.GetAddressOf(),
		m_depthStencilView.Get()
		);

	const float midnightBlue[] = { 0.098f, 0.098f, 0.439f, 1.000f };
	m_d3dContext->ClearRenderTargetView(
		m_renderTargetView.Get(),
		midnightBlue
		);

	m_d3dContext->ClearDepthStencilView(
		m_depthStencilView.Get(),
		D3D11_CLEAR_DEPTH,
		1.0f,
		0
		);


	spriteBatch->Begin();
	HRESULT result = 0;
	hResult = result;
	result = CreateDDSTextureFromMemory(m_d3dDevice.Get(), screen_buf, screen_buf_size, nullptr, m_texture1.ReleaseAndGetAddressOf());
	


	if(SUCCEEDED(result))
	{
		spriteBatch->Draw( m_texture1.Get(), XMFLOAT2(10,75), nullptr, Colors::White );
	}else{
		CreateDDSTextureFromFile( m_d3dDevice.Get(), L"Assets\\windowslogo.dds", nullptr, m_texture1.ReleaseAndGetAddressOf() );
		spriteBatch->Draw( m_texture1.Get(), XMFLOAT2(10,75), nullptr, Colors::White );
	}


	wchar_t wStr[30];
	_ltow_s(result, wStr, 20);
	m_font->DrawString( spriteBatch.get(), wStr , XMFLOAT2( 10, 10 ), Colors::Yellow );
	
	_itow_s(screen_buf_size, wStr, 10);
	m_font->DrawString( spriteBatch.get(), wStr , XMFLOAT2( 310, 200 ), Colors::Yellow );
	
	spriteBatch->End();
}

void CubeRenderer::setScreenData(const Platform::Array<byte>^ data){
	
	screen_buf_size = static_cast<int>( data->Length);
	
	if (SCREEN_BUF_MAX < screen_buf_size){
		return;
	}

    for(int i = 0 ; i < screen_buf_size; i++)
    {
		screen_buf[i] = data[i];
    }
}

int CubeRenderer::GetDebugValue(){
	return screen_buf_size;
}

